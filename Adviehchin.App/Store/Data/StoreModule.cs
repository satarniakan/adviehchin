using Adviehchin.App.Store.Entities;
using Adviehchin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Adviehchin.App.Store.Data;

/// <summary>ثبت جدول‌های فروشگاه در مدل مشترک، از نقطهٔ اتصال رسمی فریم‌ورک.</summary>
public sealed class StoreModule : IAdviehchinModule
{
    public string Name => "Store";

    public void ConfigureModel(ModelBuilder builder)
    {
        builder.Entity<StoreCategory>(e =>
        {
            e.ToTable("StoreCategories");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.SortOrder });
            e.HasMany(x => x.Products)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StoreProduct>(e =>
        {
            e.ToTable("StoreProducts");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Unit).HasMaxLength(50).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(64);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Sku).IsUnique().HasFilter("[Sku] IS NOT NULL");
            e.HasIndex(x => new { x.IsActive, x.CategoryId });
            e.HasIndex(x => x.IsFeatured);
            e.HasMany(x => x.Images)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StoreProductImage>(e =>
        {
            e.ToTable("StoreProductImages");
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.ProductId, x.SortOrder });
        });

        builder.Entity<StoreOrder>(e =>
        {
            e.ToTable("StoreOrders");
            e.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Province).HasMaxLength(80).IsRequired();
            e.Property(x => x.City).HasMaxLength(80).IsRequired();
            e.Property(x => x.Address).HasMaxLength(600).IsRequired();
            e.Property(x => x.PostalCode).HasMaxLength(15);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasMany(x => x.Items)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StoreOrderItem>(e =>
        {
            e.ToTable("StoreOrderItems");
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(50).IsRequired();
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.HasIndex(x => x.OrderId);
            e.HasOne<StoreProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
