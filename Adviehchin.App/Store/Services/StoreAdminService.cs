using Adviehchin.App.Store.Entities;
using Adviehchin.App.Store.Models;
using Adviehchin.Application.DTOs;
using Adviehchin.Domain.Exceptions;
using Adviehchin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adviehchin.App.Store.Services;

public interface IStoreAdminService
{
    Task<PagedResult<StoreProduct>> GetProductsAsync(string? search, int page, int pageSize);
    Task<ProductAdminInput?> GetProductInputAsync(int id);
    Task<int> SaveProductAsync(ProductAdminInput input);
    Task DeleteProductAsync(int id);

    Task<IReadOnlyList<StoreCategory>> GetCategoriesAsync();
    Task<CategoryAdminInput?> GetCategoryInputAsync(int id);
    Task<int> SaveCategoryAsync(CategoryAdminInput input);
    Task DeleteCategoryAsync(int id);
}

public sealed class StoreAdminService : IStoreAdminService
{
    private readonly IServiceScopeFactory _scopes;

    public StoreAdminService(IServiceScopeFactory scopes) => _scopes = scopes;

    public async Task<PagedResult<StoreProduct>> GetProductsAsync(string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var query = db.Set<StoreProduct>().AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Sku != null && p.Sku.Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<StoreProduct>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductAdminInput?> GetProductInputAsync(int id)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var product = await db.Set<StoreProduct>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return null;

        return new ProductAdminInput
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            CategoryId = product.CategoryId,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            PriceTomans = product.PriceTomans,
            DiscountPriceTomans = product.DiscountPriceTomans,
            Unit = product.Unit,
            StockQuantity = product.StockQuantity,
            Sku = product.Sku,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured
        };
    }

    public async Task<int> SaveProductAsync(ProductAdminInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BusinessRuleException("نام محصول الزامی است.");
        if (input.PriceTomans <= 0)
            throw new BusinessRuleException("قیمت محصول باید بزرگ‌تر از صفر باشد.");
        if (input.DiscountPriceTomans is not null &&
            (input.DiscountPriceTomans <= 0 || input.DiscountPriceTomans >= input.PriceTomans))
            throw new BusinessRuleException("قیمت با تخفیف باید مثبت و کمتر از قیمت اصلی باشد.");
        if (input.StockQuantity < 0)
            throw new BusinessRuleException("موجودی نمی‌تواند منفی باشد.");

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? StoreText.Slugify(input.Name) : StoreText.Slugify(input.Slug);
        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleException("نشانی (slug) محصول معتبر نیست.");

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();

        if (!await db.Set<StoreCategory>().AnyAsync(c => c.Id == input.CategoryId))
            throw new BusinessRuleException("دستهٔ انتخاب‌شده معتبر نیست.");

        if (await db.Set<StoreProduct>().AnyAsync(p => p.Slug == slug && p.Id != input.Id))
            throw new BusinessRuleException("محصول دیگری با این نشانی (slug) ثبت شده است.");

        StoreProduct product;
        if (input.Id == 0)
        {
            product = new StoreProduct();
            db.Set<StoreProduct>().Add(product);
        }
        else
        {
            product = await db.Set<StoreProduct>().FirstOrDefaultAsync(p => p.Id == input.Id)
                ?? throw new NotFoundException("محصول", input.Id.ToString());
        }

        product.Name = input.Name.Trim();
        product.Slug = slug;
        product.CategoryId = input.CategoryId;
        product.ShortDescription = input.ShortDescription?.Trim();
        product.Description = input.Description?.Trim();
        product.PriceTomans = input.PriceTomans;
        product.DiscountPriceTomans = input.DiscountPriceTomans;
        product.Unit = string.IsNullOrWhiteSpace(input.Unit) ? "۱۰۰ گرم" : input.Unit.Trim();
        product.StockQuantity = input.StockQuantity;
        product.Sku = string.IsNullOrWhiteSpace(input.Sku) ? null : input.Sku.Trim();
        product.ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? null : input.ImageUrl.Trim();
        product.IsActive = input.IsActive;
        product.IsFeatured = input.IsFeatured;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new BusinessRuleException("ذخیرهٔ محصول ناموفق بود؛ مقدارهای تکراری (نشانی/SKU) را بررسی کنید.");
        }

        return product.Id;
    }

    public async Task DeleteProductAsync(int id)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var product = await db.Set<StoreProduct>().Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("محصول", id.ToString());

        db.Set<StoreProductImage>().RemoveRange(product.Images);
        db.Set<StoreProduct>().Remove(product);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<StoreCategory>> GetCategoriesAsync()
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        return await db.Set<StoreCategory>().AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<CategoryAdminInput?> GetCategoryInputAsync(int id)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var category = await db.Set<StoreCategory>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return null;

        return new CategoryAdminInput
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    public async Task<int> SaveCategoryAsync(CategoryAdminInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BusinessRuleException("نام دسته الزامی است.");

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? StoreText.Slugify(input.Name) : StoreText.Slugify(input.Slug);
        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleException("نشانی (slug) دسته معتبر نیست.");

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();

        if (await db.Set<StoreCategory>().AnyAsync(c => c.Slug == slug && c.Id != input.Id))
            throw new BusinessRuleException("دستهٔ دیگری با این نشانی (slug) ثبت شده است.");

        StoreCategory category;
        if (input.Id == 0)
        {
            category = new StoreCategory();
            db.Set<StoreCategory>().Add(category);
        }
        else
        {
            category = await db.Set<StoreCategory>().FirstOrDefaultAsync(c => c.Id == input.Id)
                ?? throw new NotFoundException("دسته", input.Id.ToString());
        }

        category.Name = input.Name.Trim();
        category.Slug = slug;
        category.Description = input.Description?.Trim();
        category.ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? null : input.ImageUrl.Trim();
        category.SortOrder = input.SortOrder;
        category.IsActive = input.IsActive;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new BusinessRuleException("ذخیرهٔ دسته ناموفق بود؛ نشانی تکراری را بررسی کنید.");
        }

        return category.Id;
    }

    public async Task DeleteCategoryAsync(int id)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();
        var category = await db.Set<StoreCategory>().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("دسته", id.ToString());

        if (await db.Set<StoreProduct>().AnyAsync(p => p.CategoryId == id))
            throw new BusinessRuleException("این دسته محصول دارد؛ ابتدا محصولات را جابه‌جا یا حذف کنید.");

        db.Set<StoreCategory>().Remove(category);
        await db.SaveChangesAsync();
    }
}
