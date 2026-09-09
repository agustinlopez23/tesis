namespace Marketplace.DTOs;

public sealed record RegisterOwnerRequest(string? Email, string? DisplayName, string? Password);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record OwnerIdentityResponse(Guid Id, string Email, string DisplayName, string[] Roles);
