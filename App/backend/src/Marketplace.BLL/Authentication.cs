using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

public enum RegistrationStatus { Created, Invalid, Duplicate }
public enum PasswordStatus { Invalid, Valid, NeedsRehash }

public sealed record RegistrationResult(RegistrationStatus Status, OwnerIdentityResponse? Owner = null);
public sealed record LoginResult(OwnerIdentityResponse Owner, string Token, DateTimeOffset ExpiresAt);

public interface IPasswordService
{
    string Hash(UserAccount account, string password);
    PasswordStatus Verify(UserAccount account, string password);
}

public interface IIdentityRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task AddOwnerAsync(UserAccount account, CancellationToken cancellationToken);
    Task<UserAccount?> FindAccountAsync(string email, CancellationToken cancellationToken);
    Task SavePasswordHashAsync(UserAccount account, CancellationToken cancellationToken);
    Task AddSessionAsync(UserSession session, CancellationToken cancellationToken);
    Task<UserSession?> FindActiveSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task RevokeSessionAsync(UserSession session, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordFailedLoginAsync(Guid accountId, int maxAttempts, DateTimeOffset lockedUntil, CancellationToken cancellationToken);
    Task ResetFailedLoginsAsync(Guid accountId, CancellationToken cancellationToken);
}

public sealed class AuthenticationService(IIdentityRepository repository, IPasswordService passwords)
{
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<RegistrationResult> RegisterAsync(
        RegisterOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var displayName = request.DisplayName?.Trim() ?? "";
        var password = request.Password ?? "";

        if (!IsValidEmail(email) || email.Length > 254 || displayName.Length is < 1 or > 120 ||
            password.Length is < 12 or > 128)
            return new(RegistrationStatus.Invalid);

        if (await repository.EmailExistsAsync(email, cancellationToken))
            return new(RegistrationStatus.Duplicate);

        var account = new UserAccount
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = "pending"
        };
        account.PasswordHash = passwords.Hash(account, password);

        try
        {
            await repository.AddOwnerAsync(account, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return new(RegistrationStatus.Duplicate);
        }

        return new(RegistrationStatus.Created,
            new(account.Id, account.Email, account.DisplayName, ["business_owner"]));
    }

    private static bool IsValidEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { return new MailAddress(value).Address == value; }
        catch (FormatException) { return false; }
    }

    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var password = request.Password ?? "";
        var account = await repository.FindAccountAsync(email, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        // Locked accounts get the same generic rejection so lockout does not reveal valid emails.
        if (account is null || account.IsSuspended || account.LockedUntil > now) return null;

        var verification = passwords.Verify(account, password);
        if (verification == PasswordStatus.Invalid)
        {
            await repository.RecordFailedLoginAsync(account.Id, MaxFailedLogins, now.Add(LockoutDuration), cancellationToken);
            return null;
        }
        if (account.FailedLoginCount > 0 || account.LockedUntil is not null)
            await repository.ResetFailedLoginsAsync(account.Id, cancellationToken);
        if (verification == PasswordStatus.NeedsRehash)
        {
            account.PasswordHash = passwords.Hash(account, password);
            await repository.SavePasswordHashAsync(account, cancellationToken);
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expiresAt = now.AddHours(8);
        await repository.AddSessionAsync(new UserSession
        {
            UserId = account.Id,
            TokenHash = HashToken(token),
            ExpiresAt = expiresAt
        }, cancellationToken);

        return new(ToResponse(account), token, expiresAt);
    }

    public async Task<OwnerIdentityResponse?> GetSessionAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var session = await repository.FindActiveSessionAsync(HashToken(token), DateTimeOffset.UtcNow, cancellationToken);
        return session is null || session.User.IsSuspended ? null : ToResponse(session.User);
    }

    public async Task LogoutAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var now = DateTimeOffset.UtcNow;
        var session = await repository.FindActiveSessionAsync(HashToken(token), now, cancellationToken);
        if (session is not null) await repository.RevokeSessionAsync(session, now, cancellationToken);
    }

    private static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static OwnerIdentityResponse ToResponse(UserAccount account) =>
        new(account.Id, account.Email, account.DisplayName,
            account.Roles.Select(x => x.Role.Name).OrderBy(x => x).ToArray());
}

public sealed class DuplicateEmailException : Exception;
