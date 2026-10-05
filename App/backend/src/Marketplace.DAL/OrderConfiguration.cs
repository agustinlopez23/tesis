using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

internal static class OrderConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<CustomerOrder>(e =>
        {
            e.ToTable("customer_orders", t => {
                t.HasCheckConstraint("CK_order_status", "status IN ('pending','in_attention','cancelled')");
                t.HasCheckConstraint("CK_order_source", "source = 'catalog'");
                t.HasCheckConstraint("CK_order_version", "version > 0");
                t.HasCheckConstraint("CK_order_hashes", "access_key_hash ~ '^[0-9a-f]{64}$' AND request_hash ~ '^[0-9a-f]{64}$'");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.BusinessId).HasColumnName("business_id");
            e.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(15);
            e.HasIndex(x => x.Reference).IsUnique();
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(80);
            e.Property(x => x.BusinessName).HasColumnName("business_name").HasMaxLength(160);
            e.Property(x => x.WhatsApp).HasColumnName("whatsapp").HasMaxLength(16);
            e.Property(x => x.AccessKeyHash).HasColumnName("access_key_hash").HasMaxLength(64);
            e.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
            e.Property(x => x.AccessExpiresAt).HasColumnName("access_expires_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.ContactReceivedAt).HasColumnName("contact_received_at");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.Source).HasColumnName("source").HasMaxLength(20);
            e.Property(x => x.Version).HasColumnName("version");
            e.HasIndex(x => new { x.BusinessId, x.CreatedAt, x.Id });
            e.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<OrderLine>(e =>
        {
            e.ToTable("order_lines", t => {
                t.HasCheckConstraint("CK_order_line_quantity", "quantity BETWEEN 1 AND 99");
                t.HasCheckConstraint("CK_order_line_price", "unit_price >= 0");
                t.HasCheckConstraint("CK_order_line_currency", "currency IN ('ARS','USD')");
            });
            e.HasKey(x => new { x.OrderId, x.ProductId });
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(160);
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
            e.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.ProductVersion).HasColumnName("product_version");
            e.HasOne<CustomerOrder>().WithMany(x => x.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CatalogProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<OrderChange>(e =>
        {
            e.ToTable("order_changes");
            e.HasKey(x => new { x.OrderId, x.Version });
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.Version).HasColumnName("version");
            e.Property(x => x.ActorId).HasColumnName("actor_id");
            e.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            e.Property(x => x.FromStatus).HasColumnName("from_status").HasMaxLength(20);
            e.Property(x => x.ToStatus).HasColumnName("to_status").HasMaxLength(20);
            e.Property(x => x.ContactReceived).HasColumnName("contact_received");
            e.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
