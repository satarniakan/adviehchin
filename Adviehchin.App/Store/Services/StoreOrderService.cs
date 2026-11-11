using Adviehchin.App.Store.Entities;
using Adviehchin.App.Store.Models;
using Adviehchin.Application.DTOs;
using Adviehchin.Application.Services;
using Adviehchin.Domain.Exceptions;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adviehchin.App.Store.Services;

public interface IStoreOrderService
{
    Task<StoreOrder> CreateAsync(CheckoutModel form, string? userId, IReadOnlyList<CartLine> lines);
    Task<StoreOrder?> GetByNumberAsync(string orderNumber);
    Task<StoreOrder?> GetByIdAsync(int id);
    Task<IReadOnlyList<StoreOrder>> GetForUserAsync(string userId);
    Task<PagedResult<StoreOrder>> GetPagedAsync(StoreOrderStatus? status, string? search, int page, int pageSize);
    Task<StoreOrder> MarkPaidAsync(string orderNumber);
    Task UpdateStatusAsync(int id, StoreOrderStatus status);
}

public sealed class StoreOrderService : IStoreOrderService
{
    public const long FreeShippingThresholdTomans = 2_000_000;
    public const long ShippingCostTomans = 45_000;

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _clock;

    public StoreOrderService(IServiceScopeFactory scopes, TimeProvider clock)
    {
        _scopes = scopes;
        _clock = clock;
    }

    public async Task<StoreOrder> CreateAsync(CheckoutModel form, string? userId, IReadOnlyList<CartLine> lines)
    {
        if (lines is null || lines.Count == 0)
            throw new BusinessRuleException("سبد خرید خالی است.");

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAdviehchinUnitOfWork>();
        var numbers = scope.ServiceProvider.GetRequiredService<INumberSeries>();

        StoreOrder? created = null;

        await uow.ExecuteInTransactionAsync(async () =>
        {
            var ids = lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await db.Set<StoreProduct>()
                .Include(p => p.Variants)
                .Where(p => ids.Contains(p.Id) && p.IsActive)
                .ToDictionaryAsync(p => p.Id);

            var items = new List<StoreOrderItem>();
            long subtotal = 0;

            foreach (var line in lines)
            {
                if (line.Quantity is < 1 or > 99)
                    throw new BusinessRuleException("تعداد هر محصول باید بین ۱ تا ۹۹ باشد.");

                if (!products.TryGetValue(line.ProductId, out var product))
                    throw new BusinessRuleException("یکی از محصولات سبد خرید دیگر موجود نیست. لطفاً سبد را بازبینی کنید.");

                StoreProductVariant? variant = null;
                if (line.VariantId is int variantId)
                {
                    variant = product.Variants.FirstOrDefault(v => v.Id == variantId && v.IsActive)
                        ?? throw new BusinessRuleException(
                            $"گزینهٔ انتخاب‌شده برای «{product.Name}» دیگر موجود نیست. لطفاً دوباره انتخاب کنید.");
                }
                else if (product.Variants.Any(v => v.IsActive))
                {
                    throw new BusinessRuleException(
                        $"برای «{product.Name}» ابتدا وزن را انتخاب کنید.");
                }

                var available = variant?.StockQuantity ?? product.StockQuantity;
                if (line.Quantity > available)
                {
                    var label = variant is null ? product.Name : $"{product.Name} – {variant.Title}";
                    throw new BusinessRuleException($"موجودی «{label}» کافی نیست (حداکثر {available} عدد).");
                }

                var unitPrice = variant?.EffectivePriceTomans ?? product.EffectivePriceTomans;
                var lineTotal = unitPrice * line.Quantity;
                subtotal += lineTotal;

                items.Add(new StoreOrderItem
                {
                    ProductId = product.Id,
                    VariantId = variant?.Id,
                    ProductName = product.Name,
                    VariantTitle = variant?.Title,
                    Unit = product.Unit,
                    Packaging = StorePackaging.Normalize(line.Packaging),
                    ImageUrl = product.ImageUrl,
                    UnitPriceTomans = unitPrice,
                    Quantity = line.Quantity,
                    LineTotalTomans = lineTotal
                });

                if (variant is null)
                {
                    product.StockQuantity -= line.Quantity;
                }
                else
                {
                    variant.StockQuantity -= line.Quantity;
                    product.StockQuantity = AggregateStock(product);
                }
            }

            var shipping = subtotal >= FreeShippingThresholdTomans ? 0 : ShippingCostTomans;
            var orderNumber = await numbers.NextFormattedAsync("store-order", "AD-", 5);

            // درگاه آزمایشی همیشه موفق است؛ پس سفارش آنلاین همان لحظه «پرداخت‌شده» ثبت می‌شود.
            var paidOnline = form.PaymentMethod == StorePaymentMethod.OnlineGateway;

            created = new StoreOrder
            {
                OrderNumber = orderNumber,
                UserId = userId,
                CustomerName = form.CustomerName.Trim(),
                Phone = form.Phone.Trim(),
                Province = form.Province.Trim(),
                City = form.City.Trim(),
                Address = form.Address.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(form.PostalCode) ? null : form.PostalCode.Trim(),
                Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(),
                PaymentMethod = form.PaymentMethod,
                Status = paidOnline ? StoreOrderStatus.Paid : StoreOrderStatus.Processing,
                PaidAtUtc = paidOnline ? _clock.GetUtcNow().UtcDateTime : null,
                SubtotalTomans = subtotal,
                ShippingTomans = shipping,
                TotalTomans = subtotal + shipping,
                Items = items
            };

            db.Set<StoreOrder>().Add(created);
            await uow.CompleteAsync();
        });

        return created!;
    }

    /// <summary>
    /// موجودی نمایشی محصول چندگزینه‌ای = بیشترین موجودی گزینه‌های فعال. عدد تجمیعی
    /// معنا ندارد چون منبع فقط «موجود/ناموجود» می‌دهد؛ صفر یعنی همهٔ گزینه‌ها تمام شده‌اند.
    /// </summary>
    private static int AggregateStock(StoreProduct product) =>
        product.Variants
            .Where(v => v.IsActive && v.StockQuantity > 0)
            .Select(v => v.StockQuantity)
            .DefaultIfEmpty(0)
            .Max();

    public async Task<StoreOrder?> GetByNumberAsync(string orderNumber)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        return await db.Set<StoreOrder>().AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<StoreOrder?> GetByIdAsync(int id)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        return await db.Set<StoreOrder>().AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IReadOnlyList<StoreOrder>> GetForUserAsync(string userId)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        return await db.Set<StoreOrder>().AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<PagedResult<StoreOrder>> GetPagedAsync(StoreOrderStatus? status, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var query = db.Set<StoreOrder>().AsNoTracking().Include(o => o.Items).AsQueryable();

        if (status is not null)
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                o.CustomerName.Contains(term) ||
                o.Phone.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<StoreOrder>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StoreOrder> MarkPaidAsync(string orderNumber)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var order = await db.Set<StoreOrder>().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber)
            ?? throw new NotFoundException("سفارش", orderNumber);

        if (order.Status == StoreOrderStatus.Cancelled)
            throw new BusinessRuleException("سفارش لغو شده قابل پرداخت نیست.");

        if (order.Status == StoreOrderStatus.PendingPayment)
        {
            order.Status = StoreOrderStatus.Paid;
            order.PaidAtUtc = _clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync();
        }

        return order;
    }

    public async Task UpdateStatusAsync(int id, StoreOrderStatus status)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var order = await db.Set<StoreOrder>().FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException("سفارش", id.ToString());

        order.Status = status;
        if (status == StoreOrderStatus.Paid && order.PaidAtUtc is null)
            order.PaidAtUtc = _clock.GetUtcNow().UtcDateTime;

        await db.SaveChangesAsync();
    }
}
