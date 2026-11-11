namespace Adviehchin.App.Store.Sync;

/// <summary>دسته در منبع همگام‌سازی.</summary>
public sealed record SourceCategory(long Id, string Name, string Slug, string? Description);

/// <summary>مقدار یک ویژگی محصول در منبع (مثل «۱۰۰ گرم» با کد <c>100g</c>).</summary>
public sealed record SourceTerm(string Name, string Slug);

/// <summary>ویژگی محصول در منبع؛ <paramref name="Taxonomy"/> مثل <c>pa_weight</c> است.</summary>
public sealed record SourceAttribute(string Name, string Taxonomy, IReadOnlyList<SourceTerm> Terms);

/// <summary>ارجاع محصول به دسته (شناسه و کد کوتاه منبع).</summary>
public sealed record SourceCategoryRef(long Id, string Slug);

/// <summary>محصول در منبع؛ قیمت‌ها همان لحظهٔ خواندن به تومان تبدیل شده‌اند.</summary>
public sealed record SourceProduct(
    long Id,
    string Name,
    string Slug,
    string? Sku,
    string? ShortDescription,
    string? Description,
    string Type,
    bool InStock,
    long PriceTomans,
    long? DiscountPriceTomans,
    double Rating,
    int ReviewCount,
    string? Permalink,
    IReadOnlyList<SourceCategoryRef> Categories,
    IReadOnlyList<SourceAttribute> Attributes);

/// <summary>یک گزینهٔ خرید از صفحهٔ محصول منبع، همراه با عنوان و ویژگی‌های نمایشیِ فارسی.</summary>
public sealed record SourceVariation(
    long ExternalId,
    string? Sku,
    long PriceTomans,
    long? DiscountPriceTomans,
    bool InStock,
    bool IsActive,
    string Title,
    string OptionsJson,
    int SortOrder);
