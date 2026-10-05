namespace Marketplace.DTOs;

public sealed record CatalogDraftRequest(string? Facts, bool Consent);
public sealed record CatalogDraft(string Name, string Description, string Category);
public sealed record CatalogDraftAvailability(bool Enabled, string? Provider);
