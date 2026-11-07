using Adviehchin.Domain.Common;

namespace Adviehchin.App.Store.Entities;

/// <summary>
/// یک گزینهٔ قابل انتخاب از محصول (مثلاً «۱۰۰ گرم / سلفون»). قیمت هر گزینه
/// مستقل است؛ محصولِ بدون گزینه قیمت خودش را دارد و واریانت نمی‌سازد.
/// </summary>
public class StoreProductVariant : AuditableEntity
{
    public int ProductId { get; set; }
    public StoreProduct? Product { get; set; }

    /// <summary>شناسهٔ گزینه در منبع همگام‌سازی.</summary>
    public long ExternalId { get; set; }

    /// <summary>عنوان نمایشی از ترکیب ویژگی‌ها، مثل «۱۰۰ گرم / سلفون».</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>ویژگی‌های انتخاب‌شده به‌صورت JSON: <c>{"وزن":"۱۰۰ گرم","بسته بندی":"سلفون"}</c>.</summary>
    public string OptionsJson { get; set; } = "{}";

    public long PriceTomans { get; set; }
    public long? DiscountPriceTomans { get; set; }

    public int StockQuantity { get; set; }
    public string? Sku { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public bool IsOnSale => DiscountPriceTomans is > 0 && DiscountPriceTomans < PriceTomans;
    public long EffectivePriceTomans => IsOnSale ? DiscountPriceTomans!.Value : PriceTomans;
    public int DiscountPercent => IsOnSale && PriceTomans > 0
        ? (int)Math.Round((PriceTomans - DiscountPriceTomans!.Value) * 100d / PriceTomans)
        : 0;
    public bool InStock => StockQuantity > 0;
}
