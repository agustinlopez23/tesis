using System.Globalization;
using System.Text;
using Marketplace.DTOs;

namespace Marketplace.BLL;

public enum AdminStatus { Saved, Invalid, NotFound, Conflict }
public enum ModerationTarget { Account, Business, Product }
public sealed record AdminCategoryResult(AdminStatus Status, AdminCategoryResponse? Category = null);

public interface IAdminRepository
{
    Task<IReadOnlyList<AdminCategoryResponse>> CategoriesAsync(CancellationToken ct);
    Task<AdminCategoryResult> CreateCategoryAsync(Guid actor, string name, string slug, CancellationToken ct);
    Task<AdminCategoryResult> UpdateCategoryAsync(Guid actor, short id, string name, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<ModeratedAccountResponse>> AccountsAsync(string query, int page, CancellationToken ct);
    Task<IReadOnlyList<ModeratedBusinessResponse>> BusinessesAsync(string query, int page, CancellationToken ct);
    Task<IReadOnlyList<ModeratedProductResponse>> ProductsAsync(string query, int page, CancellationToken ct);
    Task<AdminStatus> ModerateAsync(Guid actor, ModerationTarget target, Guid id, bool suspend, string reason, CancellationToken ct);
    Task<AdminAuditPage> AuditAsync(int page, CancellationToken ct);
}

public sealed class AdminService(IAdminRepository repository)
{
    public Task<IReadOnlyList<AdminCategoryResponse>> CategoriesAsync(CancellationToken ct) => repository.CategoriesAsync(ct);

    public Task<AdminCategoryResult> CreateCategoryAsync(Guid actor, SaveCategoryRequest request, CancellationToken ct)
    {
        var name = CategoryName(request.Name);
        var slug = name is null ? "" : Slug(name);
        return name is null || slug.Length == 0
            ? Task.FromResult(new AdminCategoryResult(AdminStatus.Invalid))
            : repository.CreateCategoryAsync(actor, name, slug, ct);
    }

    public Task<AdminCategoryResult> UpdateCategoryAsync(Guid actor, short id, SaveCategoryRequest request, CancellationToken ct)
    {
        var name = CategoryName(request.Name);
        return name is null || request.IsActive is null
            ? Task.FromResult(new AdminCategoryResult(AdminStatus.Invalid))
            : repository.UpdateCategoryAsync(actor, id, name, request.IsActive.Value, ct);
    }

    public Task<IReadOnlyList<ModeratedAccountResponse>> AccountsAsync(string query, int page, CancellationToken ct) => repository.AccountsAsync(query, page, ct);
    public Task<IReadOnlyList<ModeratedBusinessResponse>> BusinessesAsync(string query, int page, CancellationToken ct) => repository.BusinessesAsync(query, page, ct);
    public Task<IReadOnlyList<ModeratedProductResponse>> ProductsAsync(string query, int page, CancellationToken ct) => repository.ProductsAsync(query, page, ct);

    public Task<AdminStatus> ModerateAsync(Guid actor, ModerationTarget target, Guid id, bool suspend, AdminReasonRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim() ?? "";
        return reason.Length is < 3 or > 500 || reason.Any(char.IsControl)
            ? Task.FromResult(AdminStatus.Invalid)
            : repository.ModerateAsync(actor, target, id, suspend, reason, ct);
    }

    public Task<AdminAuditPage> AuditAsync(int page, CancellationToken ct) => repository.AuditAsync(page, ct);

    public static ModerationTarget? Target(string value) => value switch
    {
        "accounts" => ModerationTarget.Account,
        "businesses" => ModerationTarget.Business,
        "products" => ModerationTarget.Product,
        _ => null
    };

    private static string? CategoryName(string? value)
    {
        var name = string.Join(' ', (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return name.Length is < 2 or > 80 || name.Any(char.IsControl) ? null : name;
    }

    // Accent-free, lowercase, hyphen-separated identifier stable across renames.
    public static string Slug(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9') builder.Append(character);
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }
        return builder.ToString().Trim('-');
    }
}
