using Adviehchin.App.Store.Entities;
using Adviehchin.App.Store.Models;
using Adviehchin.Domain.Exceptions;
using Adviehchin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Adviehchin.App.Store.Sync;

/// <summary>خلاصهٔ یک اجرای همگام‌سازی؛ برای نمایش در پنل مدیریت و لاگ.</summary>
public sealed record StoreCatalogSyncResult(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    bool IsFirstSync,
    int CategoriesCreated,
    int CategoriesUpdated,
    int CategoriesDeactivated,
    int ProductsCreated,
    int ProductsUpdated,
    int ProductsDeactivated,
    int VariantsCreated,
    int VariantsUpdated,
    int VariantsDeactivated,
    int SkippedProducts,
    IReadOnlyList<string> Warnings)
{
    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;

    public bool HasWarnings => Warnings.Count > 0;
}

public interface IStoreCatalogSyncService
{
    /// <summary>
    /// کاتالوگ منبع را می‌خواند و دسته‌ها، محصولات و گزینه‌ها را درج/به‌روزرسانی می‌کند.
    /// اجرای هم‌زمان مجاز نیست و خطای کسب‌وکار می‌دهد.
    /// </summary>
    Task<StoreCatalogSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// همگام‌سازی کاتالوگ با سایت منبع. منطق:
/// <list type="number">
/// <item>دسته‌ها بر اساس شناسهٔ منبع (و در نبود آن، کد کوتاه) درج/به‌روزرسانی می‌شوند.</item>
/// <item>محصولات بر اساس شناسهٔ منبع (و در نبود آن، SKU) تطبیق داده می‌شوند.</item>
/// <item>گزینه‌های محصولات چندقیمتی از صفحهٔ محصول خوانده و جایگزین می‌شوند.</item>
/// <item>در اولین اجرا، داده‌های نمونه/محلی غیرفعال می‌شوند تا کاتالوگ فقط منبعی شود.</item>
/// <item>قیمت/موجودی محصولات منبعی که از منبع حذف شده‌اند غیرفعال می‌شود.</item>
/// </list>
/// </summary>
public sealed class StoreCatalogSyncService : IStoreCatalogSyncService
{
    /// <summary>
    /// منبع تعداد دقیق موجودی نمی‌دهد؛ فقط «موجود/ناموجود». برای اینکه سقف سفارش
    /// سبد (۹۹) کار کند، موجودها این مقدار در نظر گرفته می‌شوند.
    /// </summary>
    private const int InStockQuantity = 99;

    /// <summary>دستهٔ «سایر محصولات» برای محصولات بدون دسته در منبع.</summary>
    private const long FallbackCategoryExternalId = 0;
    private const string FallbackCategorySlug = "other";
    private const string FallbackCategoryName = "سایر محصولات";

    private readonly IServiceScopeFactory _scopes;
    private readonly IStoreCatalogSourceClient _source;
    private readonly ILogger<StoreCatalogSyncService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public StoreCatalogSyncService(
        IServiceScopeFactory scopes,
        IStoreCatalogSourceClient source,
        ILogger<StoreCatalogSyncService> logger)
    {
        _scopes = scopes;
        _source = source;
        _logger = logger;
    }

    public async Task<StoreCatalogSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(TimeSpan.Zero, cancellationToken))
            throw new BusinessRuleException("همگام‌سازی کاتالوگ هم‌اکنون در حال اجراست؛ کمی بعد دوباره تلاش کنید.");

        try
        {
            return await RunAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<StoreCatalogSyncResult> RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var warnings = new List<string>();

        var sourceCategories = await _source.GetCategoriesAsync(cancellationToken);
        var sourceProducts = await _source.GetProductsAsync(cancellationToken);

        var variationsByProduct = new Dictionary<long, IReadOnlyList<SourceVariation>>();
        var skippedProducts = 0;

        foreach (var product in sourceProducts.Where(IsVariable))
        {
            try
            {
                var variations = await _source.GetVariationsAsync(product, cancellationToken);
                if (variations.Count == 0)
                {
                    skippedProducts++;
                    warnings.Add($"گزینه‌ای برای «{product.Name}» در صفحهٔ منبع پیدا نشد؛ قیمت قبلی حفظ شد.");
                    _logger.LogWarning("صفحهٔ محصول {Slug} گزینه‌ای نداشت", product.Slug);
                    continue;
                }

                variationsByProduct[product.Id] = variations;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                skippedProducts++;
                warnings.Add($"گزینه‌های «{product.Name}» خوانده نشد؛ قیمت قبلی حفظ شد.");
                _logger.LogWarning(ex, "خواندن گزینه‌های محصول {Slug} ناموفق بود", product.Slug);
            }
        }

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();

        var existingCategories = await db.Set<StoreCategory>().ToListAsync(cancellationToken);
        var existingProducts = await db.Set<StoreProduct>()
            .Include(p => p.Variants)
            .ToListAsync(cancellationToken);

        // اولین اجرا: هنوز هیچ محصولی از منبع ثبت نشده است
        var isFirstSync = existingProducts.All(p => p.ExternalId is null);

        var categoriesCreated = 0;
        var categoriesUpdated = 0;
        var categoriesDeactivated = 0;
        var productsCreated = 0;
        var productsUpdated = 0;
        var productsDeactivated = 0;
        var variantsCreated = 0;
        var variantsUpdated = 0;
        var variantsDeactivated = 0;

        // ── دسته‌ها ────────────────────────────────────────────────────────────
        var categoriesByExternal = existingCategories
            .Where(c => c.ExternalId.HasValue)
            .GroupBy(c => c.ExternalId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var categoriesBySlug = existingCategories
            .GroupBy(c => c.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var usedCategorySlugs = new HashSet<string>(
            existingCategories.Select(c => c.Slug), StringComparer.OrdinalIgnoreCase);
        var categoryBySourceId = new Dictionary<long, StoreCategory>();
        var seenCategoryIds = new HashSet<long>();

        for (var index = 0; index < sourceCategories.Count; index++)
        {
            var source = sourceCategories[index];
            var name = StoreSourceParsing.DecodeName(source.Name, 150);
            if (name.Length == 0) continue;

            seenCategoryIds.Add(source.Id);

            if (!categoriesByExternal.TryGetValue(source.Id, out var category) &&
                categoriesBySlug.TryGetValue(source.Slug, out var bySlug) &&
                bySlug.ExternalId is null)
            {
                // کد کوتاهِ هم‌نام (مثلاً دادهٔ نمونهٔ قدیمی) به دستهٔ منبعی تبدیل می‌شود
                category = bySlug;
            }

            if (category is null)
            {
                category = new StoreCategory
                {
                    ExternalId = source.Id,
                    Slug = UniqueSlug(source.Slug, source.Id, usedCategorySlugs, 160),
                    Name = name,
                    Description = source.Description,
                    SortOrder = index,
                    IsActive = true
                };
                db.Add(category);
                categoriesCreated++;
            }
            else
            {
                category.ExternalId = source.Id;
                category.Name = name;
                category.Description = source.Description;
                category.SortOrder = index;
                category.IsActive = true;
                categoriesUpdated++;
            }

            categoriesByExternal[source.Id] = category;
            categoriesBySlug[category.Slug] = category;
            categoryBySourceId[source.Id] = category;
        }

        // محصولات بدون دسته در منبع به این دسته می‌روند تا محدودیت «دستهٔ الزامی» نشکند
        var fallbackCategory = existingCategories.FirstOrDefault(c => c.ExternalId == FallbackCategoryExternalId);
        if (fallbackCategory is null)
        {
            fallbackCategory = new StoreCategory
            {
                ExternalId = FallbackCategoryExternalId,
                Slug = UniqueSlug(FallbackCategorySlug, FallbackCategoryExternalId + 1, usedCategorySlugs, 160),
                Name = FallbackCategoryName,
                SortOrder = 999,
                IsActive = true
            };
            db.Add(fallbackCategory);
            categoriesCreated++;
        }
        else
        {
            fallbackCategory.IsActive = true;
        }

        // ── محصولات ────────────────────────────────────────────────────────────
        var productsByExternal = existingProducts
            .Where(p => p.ExternalId.HasValue)
            .GroupBy(p => p.ExternalId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var productsBySku = existingProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
            .GroupBy(p => p.Sku!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var usedProductSlugs = new HashSet<string>(
            existingProducts.Select(p => p.Slug), StringComparer.OrdinalIgnoreCase);
        var seenProductIds = new HashSet<long>();

        foreach (var source in sourceProducts)
        {
            seenProductIds.Add(source.Id);

            var name = StoreSourceParsing.DecodeName(source.Name, 200);
            if (name.Length == 0)
            {
                warnings.Add($"محصول {source.Id} نام معتبر ندارد و رد شد.");
                continue;
            }

            var product = productsByExternal.GetValueOrDefault(source.Id);
            if (product is null && source.Sku is { Length: > 0 } &&
                productsBySku.TryGetValue(source.Sku, out var bySku))
            {
                product = bySku;
            }

            if (product is null)
            {
                product = new StoreProduct
                {
                    ExternalId = source.Id,
                    Slug = UniqueSlug(source.Slug, source.Id, usedProductSlugs, 200)
                };
                db.Add(product);
                productsCreated++;
            }
            else
            {
                productsUpdated++;
            }

            product.ExternalId = source.Id;
            product.Name = name;
            product.ShortDescription = StoreSourceParsing.HtmlToPlainText(source.ShortDescription, 500);
            product.Description = StoreSourceParsing.HtmlToPlainText(source.Description, 4000);
            product.Rating = source.Rating;
            product.ReviewCount = source.ReviewCount;
            product.IsActive = true;

            var categoryRef = source.Categories.FirstOrDefault();
            product.Category = categoryRef is not null &&
                               categoryBySourceId.TryGetValue(categoryRef.Id, out var mappedCategory)
                ? mappedCategory
                : fallbackCategory;

            // SKU یکتاست؛ اگر SKU منبع به محصول دیگری تعلق دارد، SKU فعلی حفظ می‌شود
            var sku = StoreSourceParsing.Truncate(source.Sku?.Trim(), 64);
            if (sku.Length > 0 &&
                (!productsBySku.TryGetValue(sku, out var skuOwner) || ReferenceEquals(skuOwner, product)))
            {
                product.Sku = sku;
                productsBySku[sku] = product;
            }

            if (!IsVariable(source))
            {
                product.PriceTomans = source.PriceTomans;
                product.DiscountPriceTomans = source.DiscountPriceTomans;
                product.StockQuantity = source.InStock ? InStockQuantity : 0;
                product.Unit = UnitForSimple(source);
                variantsDeactivated += DeactivateVariants(product);
            }
            else if (variationsByProduct.TryGetValue(source.Id, out var sourceVariations))
            {
                var variantResult = ApplyVariants(product, sourceVariations);
                variantsCreated += variantResult.Created;
                variantsUpdated += variantResult.Updated;
                variantsDeactivated += variantResult.Deactivated;
                ApplyVariantPriceAggregate(product, source, sourceVariations);
            }
            // صفحهٔ گزینه‌ها خوانده نشده: قیمت/موجودی/واحد قبلی دست نمی‌خورد

            productsByExternal[source.Id] = product;
        }

        // ── غیرفعال‌سازی موارد حذف‌شده ──────────────────────────────────────────
        foreach (var product in existingProducts)
        {
            if (!product.IsActive) continue;

            if (product.ExternalId is null)
            {
                if (!isFirstSync) continue; // رکوردهای محلیِ بعد از اولین همگام‌سازی دست‌نخورده می‌مانند
                product.IsActive = false;
                productsDeactivated++;
                variantsDeactivated += DeactivateVariants(product);
            }
            else if (!seenProductIds.Contains(product.ExternalId.Value))
            {
                product.IsActive = false;
                productsDeactivated++;
                variantsDeactivated += DeactivateVariants(product);
            }
        }

        foreach (var category in existingCategories)
        {
            if (!category.IsActive) continue;

            if (category.ExternalId is null)
            {
                if (!isFirstSync) continue;
                category.IsActive = false;
                categoriesDeactivated++;
            }
            else if (category.ExternalId != FallbackCategoryExternalId &&
                     !seenCategoryIds.Contains(category.ExternalId.Value))
            {
                category.IsActive = false;
                categoriesDeactivated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var result = new StoreCatalogSyncResult(
            startedAt,
            DateTimeOffset.UtcNow,
            isFirstSync,
            categoriesCreated,
            categoriesUpdated,
            categoriesDeactivated,
            productsCreated,
            productsUpdated,
            productsDeactivated,
            variantsCreated,
            variantsUpdated,
            variantsDeactivated,
            skippedProducts,
            warnings);

        _logger.LogInformation(
            "همگام‌سازی کاتالوگ تمام شد در {Seconds:0.0}s — دسته: +{CatCreated}/~{CatUpdated}/-{CatDeactivated}، " +
            "محصول: +{ProdCreated}/~{ProdUpdated}/-{ProdDeactivated}، گزینه: +{VarCreated}/~{VarUpdated}/-{VarDeactivated}",
            result.Duration.TotalSeconds,
            result.CategoriesCreated, result.CategoriesUpdated, result.CategoriesDeactivated,
            result.ProductsCreated, result.ProductsUpdated, result.ProductsDeactivated,
            result.VariantsCreated, result.VariantsUpdated, result.VariantsDeactivated);

        return result;
    }

    private static bool IsVariable(SourceProduct product)
        => string.Equals(product.Type, "variable", StringComparison.OrdinalIgnoreCase);

    private (int Created, int Updated, int Deactivated) ApplyVariants(
        StoreProduct product,
        IReadOnlyList<SourceVariation> sourceVariations)
    {
        var existing = product.Variants.ToList();
        var byExternal = existing
            .Where(v => v.ExternalId > 0)
            .GroupBy(v => v.ExternalId)
            .ToDictionary(g => g.Key, g => g.First());

        var seen = new HashSet<long>();
        var created = 0;
        var updated = 0;
        var deactivated = 0;

        foreach (var source in sourceVariations.Where(v => v.IsActive && v.ExternalId > 0))
        {
            if (!seen.Add(source.ExternalId)) continue;

            if (!byExternal.TryGetValue(source.ExternalId, out var variant))
            {
                variant = new StoreProductVariant { ExternalId = source.ExternalId };
                product.Variants.Add(variant);
                created++;
            }
            else
            {
                updated++;
            }

            variant.Title = source.Title;
            variant.OptionsJson = source.OptionsJson;
            variant.PriceTomans = source.PriceTomans;
            variant.DiscountPriceTomans = source.DiscountPriceTomans;
            variant.StockQuantity = source.InStock ? InStockQuantity : 0;
            variant.Sku = source.Sku;
            variant.IsActive = true;
            variant.SortOrder = source.SortOrder;
        }

        foreach (var variant in existing.Where(v => v.IsActive && !seen.Contains(v.ExternalId)))
        {
            variant.IsActive = false;
            deactivated++;
        }

        return (created, updated, deactivated);
    }

    private static void ApplyVariantPriceAggregate(
        StoreProduct product,
        SourceProduct source,
        IReadOnlyList<SourceVariation> sourceVariations)
    {
        var active = sourceVariations
            .Where(v => v.IsActive && v.PriceTomans > 0)
            .ToList();

        if (active.Count == 0)
        {
            product.PriceTomans = source.PriceTomans;
            product.DiscountPriceTomans = source.DiscountPriceTomans;
            product.StockQuantity = 0;
            product.Unit = "بسته";
            return;
        }

        product.PriceTomans = active.Min(v => v.PriceTomans);
        var minEffective = active.Min(EffectivePrice);
        product.DiscountPriceTomans = minEffective < product.PriceTomans ? minEffective : null;
        product.StockQuantity = active.Any(v => v.InStock) ? InStockQuantity : 0;

        var unit = active.Count > 1
            ? StoreText.PersianDigits(active.Count.ToString()) + " گزینه"
            : active[0].Title;

        if (unit == StoreSourceParsing.DefaultOptionTitle) unit = "بسته";
        product.Unit = StoreSourceParsing.Truncate(unit, 50);
    }

    private static long EffectivePrice(SourceVariation variation)
        => variation.DiscountPriceTomans is > 0 && variation.DiscountPriceTomans < variation.PriceTomans
            ? variation.DiscountPriceTomans.Value
            : variation.PriceTomans;

    private static string UnitForSimple(SourceProduct product)
    {
        var weight = product.Attributes.FirstOrDefault(a =>
            string.Equals(a.Taxonomy, "pa_weight", StringComparison.OrdinalIgnoreCase));

        var termName = weight?.Terms.FirstOrDefault()?.Name
            ?? product.Attributes.SelectMany(a => a.Terms).FirstOrDefault()?.Name;

        var unit = StoreSourceParsing.Truncate(termName, 50);
        return unit.Length > 0 ? unit : "بسته";
    }

    private static int DeactivateVariants(StoreProduct product)
    {
        var deactivated = 0;
        foreach (var variant in product.Variants.Where(v => v.IsActive))
        {
            variant.IsActive = false;
            deactivated++;
        }

        return deactivated;
    }

    /// <summary>کد کوتاه یکتا می‌سازد؛ در تداخل، شناسهٔ منبع به آن اضافه می‌شود.</summary>
    private static string UniqueSlug(string? slug, long externalId, HashSet<string> used, int maxLength)
    {
        var candidate = StoreSourceParsing.Truncate(slug?.Trim(), maxLength);
        if (candidate.Length == 0) candidate = "item-" + externalId;

        if (used.Add(candidate)) return candidate;

        var suffix = "-" + externalId;
        var room = Math.Max(1, maxLength - suffix.Length);
        var withSuffix = StoreSourceParsing.Truncate(candidate, room) + suffix;
        if (used.Add(withSuffix)) return withSuffix;

        var counter = 2;
        while (true)
        {
            var numbered = StoreSourceParsing.Truncate(candidate, room) + suffix + "-" + counter++;
            if (used.Add(numbered)) return numbered;
        }
    }
}
