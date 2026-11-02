using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

internal static class CatalogConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<BusinessSubscription>(e =>
        {
            e.ToTable("business_subscriptions", t => {
                t.HasCheckConstraint("CK_subscription_dates", "ends_at > starts_at");
                t.HasCheckConstraint("CK_subscription_revocation", "(revoked_at IS NULL AND revoked_by IS NULL AND revocation_reason IS NULL) OR (revoked_at IS NOT NULL AND revoked_by IS NOT NULL AND revocation_reason IS NOT NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.BusinessId).HasColumnName("business_id");
            e.Property(x => x.StartsAt).HasColumnName("starts_at");
            e.Property(x => x.EndsAt).HasColumnName("ends_at");
            e.Property(x => x.GrantedBy).HasColumnName("granted_by");
            e.Property(x => x.GrantedAt).HasColumnName("granted_at");
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.Property(x => x.RevokedBy).HasColumnName("revoked_by");
            e.Property(x => x.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(500);
            e.HasIndex(x => new { x.BusinessId, x.StartsAt, x.EndsAt });
            e.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.GrantedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.RevokedBy).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<CatalogProduct>(e =>
        {
            e.ToTable("catalog_products", t => {
                t.HasCheckConstraint("CK_product_price", "price >= 0");
                t.HasCheckConstraint("CK_product_status", "status IN ('draft','published','archived')");
                t.HasCheckConstraint("CK_product_currency", "currency IN ('ARS','USD')");
                t.HasCheckConstraint("CK_product_version", "version > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.BusinessId).HasColumnName("business_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(160);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(80);
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(12, 2);
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            e.Property(x => x.Available).HasColumnName("available");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.IsSuspended).HasColumnName("is_suspended");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(x => new { x.BusinessId, x.Status, x.Name, x.Id });
            e.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<ProductPhoto>(e =>
        {
            e.ToTable("product_photos", t => t.HasCheckConstraint("CK_product_photo_size", "octet_length(content) BETWEEN 1 AND 1048576"));
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Content).HasColumnName("content");
            e.HasOne<CatalogProduct>().WithOne(x => x.Photo).HasForeignKey<ProductPhoto>(x => x.ProductId);
        });
    }
}
