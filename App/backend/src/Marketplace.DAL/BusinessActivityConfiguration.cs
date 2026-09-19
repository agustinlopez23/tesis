using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketplace.DAL;

internal sealed class BusinessActivityConfiguration : IEntityTypeConfiguration<BusinessActivity>
{
    public void Configure(EntityTypeBuilder<BusinessActivity> entity)
    {
        entity.ToTable("business_activity", table => {
            table.HasCheckConstraint("CK_activity_kind", "kind IN ('impression', 'profile_view', 'contact')");
            table.HasCheckConstraint("CK_activity_surface", "surface IN ('directory', 'profile') AND (kind <> 'impression' OR surface = 'directory') AND (kind <> 'profile_view' OR surface = 'profile')");
            table.HasCheckConstraint("CK_activity_context", "(source IN ('directory', 'search') AND position BETWEEN 1 AND 200000 AND ranking_version IN ('name-v1', 'distance-v1')) OR (source = 'unknown' AND position IS NULL AND ranking_version IS NULL)");
        });
        entity.HasKey(x => new { x.VisitId, x.BusinessId, x.Kind, x.Surface });
        entity.Property(x => x.VisitId).HasColumnName("visit_id");
        entity.Property(x => x.BusinessId).HasColumnName("business_id");
        entity.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(20);
        entity.Property(x => x.Surface).HasColumnName("surface").HasMaxLength(20);
        entity.Property(x => x.Source).HasColumnName("source").HasMaxLength(20);
        entity.Property(x => x.Position).HasColumnName("position");
        entity.Property(x => x.RankingVersion).HasColumnName("ranking_version").HasMaxLength(40);
        entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        entity.HasIndex(x => new { x.BusinessId, x.OccurredAt });
        entity.HasIndex(x => x.OccurredAt);
        entity.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
    }
}
