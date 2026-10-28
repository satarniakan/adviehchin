using Adviehchin.Domain.Common;

namespace Adviehchin.App.Store.Entities;

/// <summary>دستهٔ محصولات فروشگاه (ادویه، سبزی خشک، ...).</summary>
public class StoreCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StoreProduct> Products { get; set; } = new List<StoreProduct>();
}
