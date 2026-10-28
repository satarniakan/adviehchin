using Adviehchin.Domain.Common;

namespace Adviehchin.App.Store.Entities;

public enum StorePaymentMethod
{
    CashOnDelivery = 0,
    OnlineGateway = 1
}

public enum StoreOrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

/// <summary>سفارش فروشگاه؛ شمارهٔ سند با <c>INumberSeries</c> صادر می‌شود.</summary>
public class StoreOrder : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>کاربر واردشده؛ برای خرید مهمان خالی است.</summary>
    public string? UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Note { get; set; }

    public StorePaymentMethod PaymentMethod { get; set; }
    public StoreOrderStatus Status { get; set; } = StoreOrderStatus.PendingPayment;

    public long SubtotalTomans { get; set; }
    public long ShippingTomans { get; set; }
    public long TotalTomans { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public ICollection<StoreOrderItem> Items { get; set; } = new List<StoreOrderItem>();
}

public class StoreOrderItem : AuditableEntity
{
    public int OrderId { get; set; }
    public StoreOrder? Order { get; set; }

    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public long UnitPriceTomans { get; set; }
    public int Quantity { get; set; }
    public long LineTotalTomans { get; set; }
}
