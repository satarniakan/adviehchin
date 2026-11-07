using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adviehchin.App.Store.Sync;

/// <summary>خواندن کاتالوگ از سایت منبع (فروشگاه ووکامرس).</summary>
public interface IStoreCatalogSourceClient
{
    Task<IReadOnlyList<SourceCategory>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SourceProduct>> GetProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SourceVariation>> GetVariationsAsync(SourceProduct product, CancellationToken cancellationToken);
}

/// <summary>
/// پیاده‌سازی خواندن از Store API ووکامرس. قیمت‌ها با <c>currency_minor_unit</c>
/// به تومان تبدیل می‌شوند و گزینه‌ها از JSON داخل صفحهٔ محصول استخراج می‌شوند،
/// چون Store API فهرست گزینه‌ها را عمومی منتشر نمی‌کند.
/// </summary>
public sealed class StoreCatalogSourceClient : IStoreCatalogSourceClient
{
    public const string HttpClientName = "store-catalog-source";

    /// <summary>آدرس ثابت منبعِ کاتالوگ.</summary>
    public const string DefaultBaseUrl = "https://boomrestshop.ir/";

    private const string ProductsPath = "wp-json/wc/store/v1/products";
    private const string CategoriesPath = "wp-json/wc/store/v1/products/categories";
    private const int PageSize = 100;
    private const int MaxPages = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;

    public StoreCatalogSourceClient(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    public async Task<IReadOnlyList<SourceCategory>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await GetAllAsync<CategoryDto>(CategoriesPath, cancellationToken);

        return categories
            .Where(c => c.Id > 0 && !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Slug))
            .Select(c => new SourceCategory(
                c.Id,
                WebUtility.HtmlDecode(c.Name!).Trim(),
                c.Slug!.Trim(),
                StoreSourceParsing.HtmlToPlainText(c.Description, 2000)))
            .ToList();
    }

    public async Task<IReadOnlyList<SourceProduct>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await GetAllAsync<ProductDto>(ProductsPath, cancellationToken);

        return products
            .Where(p => p.Id > 0 && !string.IsNullOrWhiteSpace(p.Name) && !string.IsNullOrWhiteSpace(p.Slug))
            .Select(MapProduct)
            .ToList();
    }

    public async Task<IReadOnlyList<SourceVariation>> GetVariationsAsync(
        SourceProduct product,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var url = string.IsNullOrWhiteSpace(product.Permalink)
            ? $"product/{Uri.EscapeDataString(product.Slug)}/"
            : product.Permalink;

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return StoreSourceParsing.ParseVariations(html, product.Attributes);
    }

    private async Task<List<TDto>> GetAllAsync<TDto>(string path, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var all = new List<TDto>();

        for (var page = 1; page <= MaxPages; page++)
        {
            using var response = await client.GetAsync(
                $"{path}?per_page={PageSize}&page={page}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var batch = await response.Content.ReadFromJsonAsync<List<TDto>>(JsonOptions, cancellationToken) ?? [];
            all.AddRange(batch);

            var totalPages = 1;
            if (response.Headers.TryGetValues("X-WP-TotalPages", out var values) &&
                int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                totalPages = parsed;
            }

            if (batch.Count == 0 || page >= totalPages) break;
        }

        return all;
    }

    private static SourceProduct MapProduct(ProductDto dto)
    {
        var minorUnit = dto.Prices?.CurrencyMinorUnit ?? 0;
        var current = ToTomans(dto.Prices?.Price, minorUnit);
        var regular = ToTomans(dto.Prices?.RegularPrice, minorUnit);
        var sale = ToTomans(dto.Prices?.SalePrice, minorUnit);

        if (regular <= 0) regular = current;
        if (current <= 0) current = regular;

        var discount = current > 0 && current < regular ? current
            : sale > 0 && sale < regular ? sale
            : (long?)null;

        var attributes = (dto.Attributes ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new SourceAttribute(
                WebUtility.HtmlDecode(a.Name!).Trim(),
                a.Taxonomy ?? string.Empty,
                (a.Terms ?? [])
                    .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                    .Select(t => new SourceTerm(
                        WebUtility.HtmlDecode(t.Name!).Trim(),
                        t.Slug ?? string.Empty))
                    .ToList()))
            .ToList();

        var categories = (dto.Categories ?? [])
            .Where(c => c.Id > 0)
            .Select(c => new SourceCategoryRef(c.Id, c.Slug ?? string.Empty))
            .ToList();

        double.TryParse(dto.AverageRating, NumberStyles.Number, CultureInfo.InvariantCulture, out var rating);

        return new SourceProduct(
            dto.Id,
            WebUtility.HtmlDecode(dto.Name!).Trim(),
            dto.Slug!.Trim(),
            string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim(),
            dto.ShortDescription,
            dto.Description,
            dto.Type ?? "simple",
            dto.IsInStock,
            regular,
            discount,
            Math.Clamp(rating, 0, 5),
            Math.Max(0, dto.ReviewCount),
            string.IsNullOrWhiteSpace(dto.Permalink) ? null : dto.Permalink,
            categories,
            attributes);
    }

    private static long ToTomans(string? amount, int minorUnit)
    {
        if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return 0;

        if (minorUnit > 0)
            value /= (decimal)Math.Pow(10, minorUnit);

        return value <= 0 ? 0 : (long)decimal.Round(value, MidpointRounding.AwayFromZero);
    }

    private sealed class CategoryDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ProductDto
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Type { get; set; }
        public string? Sku { get; set; }

        [JsonPropertyName("short_description")]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }
        public string? Permalink { get; set; }

        [JsonPropertyName("average_rating")]
        public string? AverageRating { get; set; }

        [JsonPropertyName("review_count")]
        public int ReviewCount { get; set; }

        [JsonPropertyName("is_in_stock")]
        public bool IsInStock { get; set; }

        public PriceDto? Prices { get; set; }
        public List<CategoryRefDto>? Categories { get; set; }
        public List<AttributeDto>? Attributes { get; set; }
    }

    private sealed class PriceDto
    {
        public string? Price { get; set; }

        [JsonPropertyName("regular_price")]
        public string? RegularPrice { get; set; }

        [JsonPropertyName("sale_price")]
        public string? SalePrice { get; set; }

        [JsonPropertyName("currency_minor_unit")]
        public int CurrencyMinorUnit { get; set; }
    }

    private sealed class CategoryRefDto
    {
        public long Id { get; set; }
        public string? Slug { get; set; }
    }

    private sealed class AttributeDto
    {
        public string? Name { get; set; }
        public string? Taxonomy { get; set; }
        public List<TermDto>? Terms { get; set; }
    }

    private sealed class TermDto
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
    }
}
