using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Marketplace.DAL;

public sealed class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserAccountRole> UserAccountRoles => Set<UserAccountRole>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<BusinessCategory> BusinessCategories => Set<BusinessCategory>();
    public DbSet<BusinessProfile> Businesses => Set<BusinessProfile>();
    public DbSet<BusinessPhoto> BusinessPhotos => Set<BusinessPhoto>();
    public DbSet<BusinessActivity> BusinessActivity => Set<BusinessActivity>();
    public DbSet<OwnerActivity> OwnerActivity => Set<OwnerActivity>();
    public DbSet<BusinessSubscription> BusinessSubscriptions => Set<BusinessSubscription>();
    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();
    public DbSet<ProductPhoto> ProductPhotos => Set<ProductPhoto>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderChange> OrderChanges => Set<OrderChange>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignChange> CampaignChanges => Set<CampaignChange>();
    public DbSet<CampaignEvent> CampaignEvents => Set<CampaignEvent>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<DataProtectionKeyRecord> DataProtectionKeys => Set<DataProtectionKeyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CatalogConfiguration.Configure(modelBuilder);
        OrderConfiguration.Configure(modelBuilder);
        SalesConfiguration.Configure(modelBuilder);
        CampaignConfiguration.Configure(modelBuilder);
        modelBuilder.ApplyConfiguration(new BusinessActivityConfiguration());
        modelBuilder.Entity<OwnerActivity>(entity =>
        {
            entity.ToTable("owner_activity", table => table.HasCheckConstraint("CK_owner_activity_kind",
                "kind IN ('profile_update', 'catalog_update', 'promotion_update')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(x => x.BusinessId).HasColumnName("business_id");
            entity.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(20);
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            entity.HasIndex(x => new { x.BusinessId, x.Kind, x.OccurredAt });
            entity.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DataProtectionKeyRecord>(entity =>
        {
            entity.ToTable("data_protection_keys");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FriendlyName).HasColumnName("friendly_name").HasMaxLength(256);
            entity.Property(x => x.Xml).HasColumnName("xml").IsRequired();
        });
        modelBuilder.Entity<BusinessPhoto>(entity =>
        {
            entity.ToTable("business_photos", table =>
            {
                table.HasCheckConstraint("CK_business_photos_slot", "slot BETWEEN 1 AND 5");
                table.HasCheckConstraint("CK_business_photos_size", "octet_length(content) <= 1048576");
            });
            entity.HasKey(x => new { x.BusinessId, x.Slot });
            entity.Property(x => x.BusinessId).HasColumnName("business_id");
            entity.Property(x => x.Slot).HasColumnName("slot");
            entity.Property(x => x.Caption).HasColumnName("caption").HasMaxLength(160);
            entity.Property(x => x.Width).HasColumnName("width");
            entity.Property(x => x.Height).HasColumnName("height");
            entity.Property(x => x.Content).HasColumnName("content");
            entity.HasOne<BusinessProfile>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(x => x.IsSuspended).HasColumnName("is_suspended");
            entity.Property(x => x.FailedLoginCount).HasColumnName("failed_login_count");
            entity.Property(x => x.LockedUntil).HasColumnName("locked_until");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
        modelBuilder.Entity<AdminAction>(entity =>
        {
            entity.ToTable("admin_actions", table => table.HasCheckConstraint("CK_admin_actions_target",
                "target_type IN ('account', 'business', 'product', 'category')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(x => x.ActorId).HasColumnName("actor_id");
            entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(20).IsRequired();
            entity.Property(x => x.TargetId).HasColumnName("target_id").HasMaxLength(64).IsRequired();
            entity.Property(x => x.TargetLabel).HasColumnName("target_label").HasMaxLength(254).IsRequired();
            entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.TargetType, x.TargetId });
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(new Role { Id = 1, Name = "business_owner" }, new Role { Id = 2, Name = "administrator" });
        });
        modelBuilder.Entity<UserAccountRole>(entity =>
        {
            entity.ToTable("user_account_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.RoleId).HasColumnName("role_id");
            entity.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.Accounts).HasForeignKey(x => x.RoleId);
        });
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
        modelBuilder.Entity<BusinessCategory>(entity =>
        {
            entity.ToTable("business_categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(80).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new BusinessCategory { Id = 1, Name = "Almacenes", Slug = "almacenes" },
                new BusinessCategory { Id = 2, Name = "Gastronomía", Slug = "gastronomia" },
                new BusinessCategory { Id = 3, Name = "Indumentaria", Slug = "indumentaria" });
        });
        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.ToTable("businesses", table => table.HasCheckConstraint(
                "CK_businesses_status", "status IN ('draft', 'published')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OwnerId).HasColumnName("owner_id");
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Address).HasColumnName("address").HasMaxLength(250).IsRequired();
            entity.Property(x => x.WhatsApp).HasColumnName("whatsapp").HasMaxLength(16).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(x => x.IsSuspended).HasColumnName("is_suspended");
            entity.Property(x => x.FirstPublishedAt).HasColumnName("first_published_at").ValueGeneratedOnAddOrUpdate();
            entity.Property(x => x.PublicationHistoryUnknown).HasColumnName("publication_history_unknown").ValueGeneratedOnAddOrUpdate();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.Location).HasColumnName("location")
                .HasConversion(
                    location => new Point(location.Longitude, location.Latitude) { SRID = 4326 },
                    point => new GeoLocation(point.Y, point.X))
                .HasColumnType("geography (point,4326)");
            entity.HasIndex(x => x.OwnerId);
            entity.HasIndex(x => x.Location).HasMethod("gist");
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OwnerId);
            entity.HasOne<BusinessCategory>().WithMany().HasForeignKey(x => x.CategoryId);
        });
    }
}
