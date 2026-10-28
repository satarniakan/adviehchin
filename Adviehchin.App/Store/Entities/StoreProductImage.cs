using Adviehchin.Domain.Common;

namespace Adviehchin.App.Store.Entities;

public class StoreProductImage : AuditableEntity
{
    public int ProductId { get; set; }
    public StoreProduct? Product { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
