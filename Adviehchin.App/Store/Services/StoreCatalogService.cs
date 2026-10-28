using Adviehchin.App.Store.Entities;
using Adviehchin.App.Store.Models;
using Adviehchin.Application.DTOs;
using Adviehchin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adviehchin.App.Store.Services;

public interface IStoreCatalogService
{
    Task<IReadOnlyList<StoreCategory>> GetCategoriesAsync(bool includeInactive = false);
    Task<StoreCategory?> GetCategoryAsync(string slug);
    Task<PagedResult<StoreProduct>> GetProductsAsync(StoreQuery query);
    Task<StoreProduct?> GetProductAsync(string slug);
    Task<StoreProduct?> GetProductByIdAsync(int id);
    Task<IReadOnlyList<StoreProduct>> GetFeaturedAsync(int take = 8);
    Task<IReadOnlyList<StoreProduct>> GetOnSaleAsync(int take = 4);
    Task<IReadOnlyList<StoreProduct>> GetByCategoryAsync(string categorySlug, int take = 4);
    Task<IReadOnlyList<StoreProduct>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4);
}

/// <summary>
/// کوئری‌های فقط‌خواندنی کاتالوگ. هر عملیات در scope مستقل خودش اجرا می‌شود تا
/// رندر هم‌زمان چند کامپوننت Blazor، یک DbContext مشترک را قفل نکند.
/// </summary>
public sealed class StoreCatalogService : IStoreCatalogService
{
    private readonly IServiceScopeFactory _scopes;

    public StoreCatalogService(IServiceScopeFactory scopes) => _scopes = scopes;

    private async Task<T> WithDbAsync<T>(Func<AdviehchinDbContext, Task<T>> action)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        return await action(db);
    }

    public Task<IReadOnlyList<StoreCategory>> GetCategoriesAsync(bool includeInactive = false) =>
        WithDbAsync<IReadOnlyList<StoreCategory>>(async db =>
            await db.Set<StoreCategory>()
                .AsNoTracking()
                .Where(c => includeInactive || c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync());

    public Task<StoreCategory?> GetCategoryAsync(string slug) =>
        WithDbAsync(db => db.Set<StoreCategory>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive));

    public Task<PagedResult<StoreProduct>> GetProductsAsync(StoreQuery query) =>
        WithDbAsync(async db =>
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 48);

            var products = db.Set<StoreProduct>().AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Category!.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Category))
                products = products.Where(p => p.Category!.Slug == query.Category);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                products = products.Where(p =>
                    p.Name.Contains(term) ||
                    (p.ShortDescription != null && p.ShortDescription.Contains(term)));
            }

            var total = await products.CountAsync();

            products = query.Sort switch
            {
                "price-asc" => products.OrderBy(p => p.DiscountPriceTomans > 0 && p.DiscountPriceTomans < p.PriceTomans
                    ? p.DiscountPriceTomans : p.PriceTomans),
                "price-desc" => products.OrderByDescending(p => p.DiscountPriceTomans > 0 && p.DiscountPriceTomans < p.PriceTomans
                    ? p.DiscountPriceTomans : p.PriceTomans),
                "newest" => products.OrderByDescending(p => p.Id),
                "discount" => products.OrderByDescending(p => p.DiscountPriceTomans != null && p.DiscountPriceTomans > 0),
                _ => products.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id)
            };

            var items = await products.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<StoreProduct>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        });

    public Task<StoreProduct?> GetProductAsync(string slug) =>
        WithDbAsync(db => db.Set<StoreProduct>().AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive));

    public Task<StoreProduct?> GetProductByIdAsync(int id) =>
        WithDbAsync(db => db.Set<StoreProduct>().AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id));

    public Task<IReadOnlyList<StoreProduct>> GetFeaturedAsync(int take = 8) =>
        WithDbAsync<IReadOnlyList<StoreProduct>>(async db =>
            await db.Set<StoreProduct>().AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.Id)
                .Take(take)
                .ToListAsync());

    public Task<IReadOnlyList<StoreProduct>> GetOnSaleAsync(int take = 4) =>
        WithDbAsync<IReadOnlyList<StoreProduct>>(async db =>
            await db.Set<StoreProduct>().AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.DiscountPriceTomans != null
                            && p.DiscountPriceTomans > 0 && p.DiscountPriceTomans < p.PriceTomans)
                .OrderByDescending(p => p.Id)
                .Take(take)
                .ToListAsync());

    public Task<IReadOnlyList<StoreProduct>> GetByCategoryAsync(string categorySlug, int take = 4) =>
        WithDbAsync<IReadOnlyList<StoreProduct>>(async db =>
            await db.Set<StoreProduct>().AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Category!.Slug == categorySlug)
                .OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id)
                .Take(take)
                .ToListAsync());

    public Task<IReadOnlyList<StoreProduct>> GetRelatedAsync(int categoryId, int excludeProductId, int take = 4) =>
        WithDbAsync<IReadOnlyList<StoreProduct>>(async db =>
            await db.Set<StoreProduct>().AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.CategoryId == categoryId && p.Id != excludeProductId)
                .OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id)
                .Take(take)
                .ToListAsync());
}
