using Marketplace.BLL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Marketplace.DAL;

public sealed class IdentityRepository(MarketplaceDbContext database) : IIdentityRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        database.UserAccounts.AnyAsync(x => x.Email == email, cancellationToken);

    public async Task AddOwnerAsync(UserAccount account, CancellationToken cancellationToken)
    {
        account.Roles.Add(new UserAccountRole { User = account, RoleId = 1 });
        database.UserAccounts.Add(account);
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new DuplicateEmailException();
        }
    }

    public Task<UserAccount?> FindAccountAsync(string email, CancellationToken cancellationToken) =>
        database.UserAccounts.Include(x => x.Roles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task SavePasswordHashAsync(UserAccount account, CancellationToken cancellationToken) =>
        database.SaveChangesAsync(cancellationToken);

    public async Task AddSessionAsync(UserSession session, CancellationToken cancellationToken)
    {
        database.UserSessions.Add(session);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task<UserSession?> FindActiveSessionAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        database.UserSessions.Include(x => x.User).ThenInclude(x => x.Roles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > now,
                cancellationToken);

    public async Task RevokeSessionAsync(UserSession session, DateTimeOffset now, CancellationToken cancellationToken)
    {
        session.RevokedAt = now;
        await database.SaveChangesAsync(cancellationToken);
    }

    // Single-statement increment keeps concurrent failures from losing counts.
    public Task RecordFailedLoginAsync(Guid accountId, int maxAttempts, DateTimeOffset lockedUntil, CancellationToken cancellationToken) =>
        database.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE user_accounts SET
                locked_until = CASE WHEN failed_login_count + 1 >= {maxAttempts} THEN {lockedUntil} ELSE locked_until END,
                failed_login_count = CASE WHEN failed_login_count + 1 >= {maxAttempts} THEN 0 ELSE failed_login_count + 1 END
            WHERE id = {accountId}
            """, cancellationToken);

    public Task ResetFailedLoginsAsync(Guid accountId, CancellationToken cancellationToken) =>
        database.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE user_accounts SET failed_login_count = 0, locked_until = NULL WHERE id = {accountId}", cancellationToken);
}

public sealed class DatabaseInitializer(MarketplaceDbContext database) : IDatabaseInitializer
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        database.Database.MigrateAsync(cancellationToken);
}
