using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.DTOs;

namespace Marketplace.BLL;

public sealed record CampaignContext(Guid CampaignId, Guid ContextId);
public interface ICampaignTokens
{
    string Issue(CampaignContext context);
    CampaignContext? Read(string? token);
}
public interface ICampaignRepository
{
    Task<object?> List(Guid? business, Guid actor, bool admin, int page, CancellationToken ct);
    Task<SalesResult> Save(Guid business, Guid id, Guid owner, SaveCampaignRequest body, string hash, CancellationToken ct);
    Task<SalesResult> Act(Guid? business, Guid id, Guid actor, bool admin, CampaignActionRequest body, string hash, CancellationToken ct);
    Task<IReadOnlyList<PublicCampaign>> Public(string zone, CancellationToken ct);
    Task<IReadOnlyList<PublicCampaign>> ForBusiness(Guid business, CancellationToken ct);
    Task<PublicCampaign?> Detail(Guid id, CancellationToken ct);
    Task<byte[]?> Image(Guid id, Guid? actor, bool admin, CancellationToken ct);
    Task<bool> Record(CampaignContext context, string kind, Guid? actor, CancellationToken ct);
    Task<CampaignMetrics?> Metrics(Guid business, Guid id, Guid owner, CancellationToken ct);
    Task<int> Purge(CancellationToken ct);
}
public sealed class CampaignService(ICampaignRepository repository, ICampaignTokens tokens)
{
    public static bool Zone(string? zone) => zone is "centro" or "nueva-cordoba" or "alberdi";
    private static bool Text(string? value, int min, int max) => value?.Trim().Length >= min && value.Trim().Length <= max && !value.Any(char.IsControl);
    private static string Hash(object value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    public Task<object?> List(Guid? business, Guid actor, bool admin, int page, CancellationToken ct) => repository.List(business, actor, admin, page, ct);
    public Task<SalesResult> Save(Guid business, Guid id, Guid owner, SaveCampaignRequest body, CancellationToken ct)
    {
        if (id == Guid.Empty || body.ProductId == Guid.Empty || body.Version < 0 || !Text(body.Title, 3, 160) || !Text(body.Description, 3, 1000) || !Text(body.Conditions, 3, 1000) || !Zone(body.ZoneId) ||
            body.Status is not ("draft" or "pending_review") || body.EndsAt <= body.StartsAt || body.EndsAt - body.StartsAt > TimeSpan.FromDays(30) ||
            (body.ProductId == null ? body.PhotoSlot is not (>= 1 and <= 5) : body.PhotoSlot != null)) return Task.FromResult(new SalesResult(400));
        static DateTimeOffset Precision(DateTimeOffset value) => value.ToUniversalTime().AddTicks(-(value.Ticks % 10));
        body = body with { Title = body.Title!.Trim(), Description = body.Description!.Trim(), Conditions = body.Conditions!.Trim(), StartsAt = Precision(body.StartsAt), EndsAt = Precision(body.EndsAt) };
        return repository.Save(business, id, owner, body, Hash(new { business, id, owner, body }), ct);
    }
    public Task<SalesResult> Act(Guid? business, Guid id, Guid actor, bool admin, CampaignActionRequest body, CancellationToken ct)
    {
        if (body.OperationId == Guid.Empty || body.Version < 1 || !Text(body.Reason, 3, 300) ||
            body.Action is not ("approve" or "reject" or "confirm_payment" or "pause" or "resume" or "resolve") ||
            (!admin && body.Action != "pause") || (body.Action == "confirm_payment" && !Text(body.PaymentReference, 3, 120))) return Task.FromResult(new SalesResult(400));
        body = body with { Reason = body.Reason!.Trim(), PaymentReference = body.Action == "confirm_payment" ? body.PaymentReference!.Trim() : null };
        return repository.Act(business, id, actor, admin, body, Hash(new { business, id, actor, body }), ct);
    }
    public async Task<IReadOnlyList<PublicCampaign>> Public(string zone, CancellationToken ct) =>
        (await repository.Public(zone, ct)).Select(c => c with { Token = tokens.Issue(new(c.Id, Guid.NewGuid())) }).ToArray();
    // Active promotions of one business for its public profile, in every zone; each card gets its own measurement token.
    public const int ProfileLimit = 6;
    public async Task<IReadOnlyList<PublicCampaign>> ForBusiness(Guid business, CancellationToken ct) =>
        (await repository.ForBusiness(business, ct)).Select(c => c with { Token = tokens.Issue(new(c.Id, Guid.NewGuid())) }).ToArray();
    public async Task<PublicCampaign?> Detail(Guid id, CancellationToken ct)
    {
        var campaign = await repository.Detail(id, ct);
        return campaign is null ? null : campaign with { Token = tokens.Issue(new(campaign.Id, Guid.NewGuid())) };
    }
    public Task<byte[]?> Image(Guid id, Guid? actor, bool admin, CancellationToken ct) => repository.Image(id, actor, admin, ct);
    public Task<bool> Record(CampaignContext context, string kind, Guid? actor, CancellationToken ct) => repository.Record(context, kind, actor, ct);
    public Task<CampaignMetrics?> Metrics(Guid business, Guid id, Guid owner, CancellationToken ct) => repository.Metrics(business, id, owner, ct);
    public Task<int> Purge(CancellationToken ct) => repository.Purge(ct);
}
