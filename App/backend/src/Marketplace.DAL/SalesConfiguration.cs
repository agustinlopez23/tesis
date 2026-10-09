using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Marketplace.DAL;

internal static class SalesConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<InventoryBalance>(e => {
            e.ToTable("inventory_balances", t => { t.HasCheckConstraint("CK_inventory_quantity", "quantity BETWEEN 0 AND 1000000"); t.HasCheckConstraint("CK_inventory_version", "version > 0"); });
            e.HasKey(x => x.ProductId);
            e.HasOne<CatalogProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<InventoryMovement>(e => {
            e.ToTable("inventory_movements", t => { t.HasCheckConstraint("CK_movement_balance", "balance BETWEEN 0 AND 1000000"); t.HasCheckConstraint("CK_movement_kind", "kind IN ('adjustment','sale','return')"); });
            e.HasKey(x => new { x.OperationId, x.ProductId });
            e.Property(x => x.Reason).HasMaxLength(300);
            e.Property(x => x.Kind).HasMaxLength(20);
            e.Property(x => x.RequestHash).HasMaxLength(64);
            e.HasIndex(x => new { x.BusinessId, x.CreatedAt, x.OperationId });
            e.HasOne<CatalogProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Sale>(e => {
            e.ToTable("sales"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId).IsUnique();
            e.HasIndex(x => new { x.BusinessId, x.CreatedAt, x.Id });
            e.Property(x => x.RequestHash).HasMaxLength(64); e.Property(x => x.VoidHash).HasMaxLength(64);
            e.Property(x => x.VoidReason).HasMaxLength(300);
            e.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.VoidedBy).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<SaleLine>(e => {
            e.ToTable("sale_lines", t => {
                t.HasCheckConstraint("CK_sale_quantity", "quantity BETWEEN 1 AND 99 AND returned_quantity BETWEEN 0 AND quantity");
                t.HasCheckConstraint("CK_sale_price", "unit_price >= 0 AND currency IN ('ARS','USD')");
                t.HasCheckConstraint("CK_sale_origin", "origin IN ('requested','conversation','direct')");
            });
            e.HasKey(x => new { x.SaleId, x.ProductId });
            e.Property(x => x.Name).HasMaxLength(160); e.Property(x => x.Currency).HasMaxLength(3); e.Property(x => x.Origin).HasMaxLength(20);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.HasOne<Sale>().WithMany(x => x.Lines).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CatalogProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        foreach (var type in new[] { typeof(InventoryBalance), typeof(InventoryMovement), typeof(Sale), typeof(SaleLine) })
            foreach (var property in model.Entity(type).Metadata.GetProperties())
                property.SetColumnName(Regex.Replace(property.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}
