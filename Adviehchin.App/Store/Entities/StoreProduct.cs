using Adviehchin.Domain.Common;

namespace Adviehchin.App.Store.Entities;

/// <summary>محصول قابل فروش. قیمت‌ها به تومان ذخیره می‌شوند.</summary>
public class StoreProduct : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public long PriceTomans { get; set; }
    public long? DiscountPriceTomans { get; set; }

    /// <summary>وزن/بسته‌بندی نمایشی، مثل «۱۰۰ گرم».</summary>
    public string Unit { get; set; } = "۱۰۰ گرم";

    public int StockQuantity { get; set; }
    public string? Sku { get; set; }

    public int CategoryId { get; set; }
    public StoreCategory? Category { get; set; }

    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    /// <summary>میانگین امتیاز نمایشی (نظرات واقعی در نسخهٔ بعدی).</summary>
    public double Rating { get; set; } = 4.6;
    public int ReviewCount { get; set; }

    public ICollection<StoreProductImage> Images { get; set; } = new List<StoreProductImage>();

    public bool IsOnSale => DiscountPriceTomans is > 0 && DiscountPriceTomans < PriceTomans;
    public long EffectivePriceTomans => IsOnSale ? DiscountPriceTomans!.Value : PriceTomans;
    public int DiscountPercent => IsOnSale && PriceTomans > 0
        ? (int)Math.Round((PriceTomans - DiscountPriceTomans!.Value) * 100d / PriceTomans)
        : 0;
    public bool InStock => StockQuantity > 0;
}
