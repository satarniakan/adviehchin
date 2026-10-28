using System.Text.Json;
using Adviehchin.App.Store.Entities;
using Adviehchin.App.Store.Models;
using Microsoft.JSInterop;

namespace Adviehchin.App.Store.Services;

/// <summary>
/// سبد خرید سمت مرورگر. هر مدار یک نمونهٔ مستقل دارد و محتوای آن در
/// localStorage ذخیره می‌شود؛ قیمت‌ها هنگام ثبت سفارش دوباره از دیتابیس خوانده می‌شوند.
/// </summary>
public sealed class CartState
{
    private const string StorageKey = "adviehchin.cart.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _js;
    private readonly List<CartLine> _lines = [];
    private Task? _initializeTask;

    public CartState(IJSRuntime js) => _js = js;

    public event Action? Changed;

    public IReadOnlyList<CartLine> Lines => _lines;

    public int TotalQuantity => _lines.Sum(l => l.Quantity);

    public long SubtotalTomans => _lines.Sum(l => l.LineTotalTomans);

    public bool IsEmpty => _lines.Count == 0;

    public Task InitializeAsync() => _initializeTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("storeCart.get", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var stored = JsonSerializer.Deserialize<List<CartLine>>(json, JsonOptions);
                if (stored is not null)
                {
                    _lines.Clear();
                    _lines.AddRange(stored.Where(l =>
                        l.ProductId > 0 && l.Quantity is >= 1 and <= 99 && l.UnitPriceTomans > 0));
                }
            }
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
        catch (JsonException) { }

        Notify();
    }

    public async Task AddAsync(StoreProduct product, int quantity = 1)
    {
        await InitializeAsync();
        if (product.StockQuantity <= 0 || quantity <= 0) return;

        var max = Math.Clamp(product.StockQuantity, 1, 99);
        var line = _lines.FirstOrDefault(l => l.ProductId == product.Id);
        if (line is null)
        {
            line = new CartLine { ProductId = product.Id };
            _lines.Add(line);
        }

        line.Name = product.Name;
        line.Slug = product.Slug;
        line.Unit = product.Unit;
        line.ImageUrl = product.ImageUrl;
        line.UnitPriceTomans = product.EffectivePriceTomans;
        line.StockQuantity = product.StockQuantity;
        line.Quantity = Math.Clamp(line.Quantity + quantity, 1, max);

        await PersistAsync();
    }

    public async Task SetQuantityAsync(int productId, int quantity)
    {
        await InitializeAsync();
        var line = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (line is null) return;

        if (quantity <= 0)
        {
            _lines.Remove(line);
        }
        else
        {
            line.Quantity = Math.Clamp(quantity, 1, Math.Clamp(line.StockQuantity, 1, 99));
        }

        await PersistAsync();
    }

    public async Task RemoveAsync(int productId)
    {
        await InitializeAsync();
        _lines.RemoveAll(l => l.ProductId == productId);
        await PersistAsync();
    }

    public async Task ClearAsync()
    {
        await InitializeAsync();
        _lines.Clear();
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        Notify();
        try
        {
            var json = JsonSerializer.Serialize(_lines, JsonOptions);
            await _js.InvokeVoidAsync("storeCart.set", StorageKey, json);
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
    }

    private void Notify() => Changed?.Invoke();
}
