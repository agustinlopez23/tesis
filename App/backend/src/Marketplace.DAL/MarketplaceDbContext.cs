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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(new Role { Id = 1, Name = "business_owner" });
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
            entity.HasIndex(x => x.Slug).IsUnique();
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
