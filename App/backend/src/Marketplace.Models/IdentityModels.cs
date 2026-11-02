namespace Marketplace.Models;

public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsSuspended { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<UserAccountRole> Roles { get; set; } = [];
}

public sealed class Role
{
    public short Id { get; set; }
    public required string Name { get; set; }
    public ICollection<UserAccountRole> Accounts { get; set; } = [];
}

public sealed class UserAccountRole
{
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public short RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
