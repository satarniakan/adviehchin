using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Adviehchin.App.Store.Sync;

/// <summary>
/// استخراج داده از HTML منبع: متن نمایشیِ امن (بدون تگ) و گزینه‌های محصول
/// از ویژگی <c>data-product_variations</c>. جدا شده تا بدون شبکه هم تست شود.
/// </summary>
public static partial class StoreSourceParsing
{
    /// <summary>عنوان گزینه‌ها وقتی محصول هیچ ویژگی ندارد.</summary>
    public const string DefaultOptionTitle = "گزینه";

    private static readonly Regex ScriptStyleRegex = new(
        "<(script|style)[^>]*>.*?</\\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private static readonly Regex VariationsRegex = new(
        "data-product_variations\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')",
        RegexOptions.Singleline | RegexOptions.Compiled);

    // برای ذخیرهٔ خوانا در دیتابیس؛ خروجی هنوز JSON معتبر است و در UI خودکار HTML-encode می‌شود
    private static readonly JsonSerializerOptions OptionsJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>ویژگی‌های یک گزینه را به نام فارسیِ ویژگی و مقدار تبدیل می‌کند.</summary>
    public static IReadOnlyList<SourceVariation> ParseVariations(
        string? html,
        IReadOnlyList<SourceAttribute> attributes)
    {
        if (string.IsNullOrWhiteSpace(html)) return [];

        var match = VariationsRegex.Match(html);
        if (!match.Success) return [];

        var encodedJson = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        var json = WebUtility.HtmlDecode(encodedJson);
        if (string.IsNullOrWhiteSpace(json) || json is "false" or "[]") return [];

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return [];

            var variations = new List<SourceVariation>();
            var order = 0;

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (!TryGetInt64(element, "variation_id", out var externalId) || externalId <= 0)
                    continue;

                var parsed = BuildVariation(element, externalId, attributes, order++);
                if (parsed is not null) variations.Add(parsed);
            }

            return variations;
        }
        catch (JsonException)
        {
            // صفحهٔ محصول بدون JSON معتبر (طرح متفاوت منبع) — همین محصول بدون گزینه می‌ماند
            return [];
        }
    }

    /// <summary>HTML منبع را به متن یک‌خطیِ نمایش‌پذیر تبدیل می‌کند و در صورت نیاز می‌بُرد.</summary>
    public static string? HtmlToPlainText(string? html, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var withoutScripts = ScriptStyleRegex.Replace(html, " ");
        var withoutTags = TagRegex.Replace(withoutScripts, " ");
        var text = Normalize(WebUtility.HtmlDecode(withoutTags));

        return text.Length == 0 ? null : Truncate(text, maxLength);
    }

    /// <summary>نام محصول/دسته را از HTML باز می‌کند و فاصله‌ها را یکدست می‌کند.</summary>
    public static string DecodeName(string? value, int maxLength)
        => Truncate(Normalize(WebUtility.HtmlDecode(value ?? string.Empty)), maxLength);

    /// <summary>متن را به حداکثر طول می‌رساند؛ بریدن با سه‌نقطه انجام می‌شود.</summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (maxLength <= 0 || text.Length <= maxLength) return text;

        return text[..(maxLength - 1)].TrimEnd() + "…";
    }

    private static SourceVariation? BuildVariation(
        JsonElement element,
        long externalId,
        IReadOnlyList<SourceAttribute> attributes,
        int sortOrder)
    {
        var regular = GetMoney(element, "display_regular_price");
        var current = GetMoney(element, "display_price");
        if (regular <= 0) regular = current;
        if (current <= 0) current = regular;

        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        if (element.TryGetProperty("attributes", out var attributesElement) &&
            attributesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var option in attributesElement.EnumerateObject())
            {
                var rawValue = option.Value.ValueKind == JsonValueKind.String
                    ? option.Value.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(rawValue)) continue;

                var (attributeName, termName) = ResolveOption(option.Name, rawValue, attributes);
                options[attributeName] = termName;
            }
        }

        var title = options.Count > 0
            ? Truncate(string.Join(" / ", options.Values), 200)
            : DefaultOptionTitle;

        var optionsJson = options.Count > 0
            ? JsonSerializer.Serialize(options, OptionsJsonOptions)
            : "{}";
        if (optionsJson.Length > 1000) optionsJson = "{}";

        var isActive = GetBool(element, "variation_is_active", true) &&
                       GetBool(element, "variation_is_visible", true);

        return new SourceVariation(
            externalId,
            Truncate(GetString(element, "sku"), 64) is { Length: > 0 } sku ? sku : null,
            regular,
            current > 0 && current < regular ? current : null,
            GetBool(element, "is_in_stock", false),
            isActive,
            title,
            optionsJson,
            sortOrder);
    }

    private static (string AttributeName, string TermName) ResolveOption(
        string rawKey,
        string rawValue,
        IReadOnlyList<SourceAttribute> attributes)
    {
        const string attributePrefix = "attribute_";
        var key = rawKey.StartsWith(attributePrefix, StringComparison.Ordinal)
            ? Uri.UnescapeDataString(rawKey[attributePrefix.Length..])
            : rawKey;
        var value = Uri.UnescapeDataString(rawValue);

        SourceAttribute? attribute = null;

        // ویژگی‌های ووکامرس با پیشوند pa_ می‌آیند (مثل pa_weight)
        if (key.StartsWith("pa_", StringComparison.OrdinalIgnoreCase))
        {
            var taxonomy = key[3..];
            attribute = attributes.FirstOrDefault(a =>
                string.Equals(a.Taxonomy, "pa_" + taxonomy, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Taxonomy, key, StringComparison.OrdinalIgnoreCase));
        }

        attribute ??= attributes.FirstOrDefault(a =>
            string.Equals(a.Name, key, StringComparison.OrdinalIgnoreCase));

        var term = attribute?.Terms.FirstOrDefault(t =>
            string.Equals(t.Slug, value, StringComparison.OrdinalIgnoreCase));

        return (attribute?.Name is { Length: > 0 } name ? name : key, term?.Name ?? value);
    }

    private static string Normalize(string value) => WhitespaceRegex.Replace(value, " ").Trim();

    private static bool TryGetInt64(JsonElement element, string name, out long value)
    {
        value = 0;
        if (!element.TryGetProperty(name, out var property)) return false;

        if (property.ValueKind == JsonValueKind.Number) return property.TryGetInt64(out value);
        if (property.ValueKind == JsonValueKind.String)
            return long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        return false;
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property)) return null;
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static bool GetBool(JsonElement element, string name, bool fallback)
    {
        if (!element.TryGetProperty(name, out var property)) return fallback;
        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => property.TryGetInt32(out var number) && number != 0,
            _ => fallback
        };
    }

    private static long GetMoney(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property)) return 0;

        decimal amount = 0;
        if (property.ValueKind == JsonValueKind.Number)
            property.TryGetDecimal(out amount);
        else if (property.ValueKind == JsonValueKind.String)
            decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

        return amount <= 0 ? 0 : (long)decimal.Round(amount, MidpointRounding.AwayFromZero);
    }
}
