using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Marketplace.DAL;

internal static class CampaignConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Campaign>(e => {
            e.ToTable("campaigns", t => {
                t.HasCheckConstraint("CK_campaign_status", "status IN ('draft','pending_review','awaiting_payment','scheduled','paused','rejected')");
                t.HasCheckConstraint("CK_campaign_dates", "ends_at > starts_at AND ends_at <= starts_at + interval '30 days'");
                t.HasCheckConstraint("CK_campaign_zone", "zone_id IN ('centro','nueva-cordoba','alberdi')");
                t.HasCheckConstraint("CK_campaign_destination", "(product_id IS NOT NULL AND photo_slot IS NULL) OR (product_id IS NULL AND photo_slot BETWEEN 1 AND 5)");
                t.HasCheckConstraint("CK_campaign_image", "image IS NULL OR octet_length(image) <= 1048576");
            });
            e.HasKey(x => x.Id); e.HasIndex(x => new { x.BusinessId, x.CreatedAt, x.Id }); e.HasIndex(x => new { x.ZoneId, x.StartsAt, x.EndsAt });
            e.Property(x => x.Title).HasMaxLength(160); e.Property(x => x.Description).HasMaxLength(1000); e.Property(x => x.Conditions).HasMaxLength(1000);
            e.Property(x => x.ZoneId).HasMaxLength(32); e.Property(x => x.Status).HasMaxLength(24); e.Property(x => x.SaveHash).HasMaxLength(64); e.Property(x => x.PaymentReference).HasMaxLength(120);
            e.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CatalogProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<CampaignChange>(e => {
            e.ToTable("campaign_changes"); e.HasKey(x => x.OperationId); e.HasIndex(x => new { x.CampaignId, x.Version }).IsUnique();
            e.Property(x => x.Action).HasMaxLength(24); e.Property(x => x.Reason).HasMaxLength(300); e.Property(x => x.RequestHash).HasMaxLength(64);
            e.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<CampaignEvent>(e => {
            e.ToTable("campaign_events", t => t.HasCheckConstraint("CK_campaign_event_kind", "kind IN ('impression','click')"));
            e.HasKey(x => new { x.CampaignId, x.ContextId, x.Kind }); e.Property(x => x.Kind).HasMaxLength(16); e.HasIndex(x => x.CreatedAt);
            e.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
        });
        foreach (var type in new[] { typeof(Campaign), typeof(CampaignChange), typeof(CampaignEvent) })
            foreach (var property in model.Entity(type).Metadata.GetProperties())
                property.SetColumnName(Regex.Replace(property.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}
