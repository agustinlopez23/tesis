using System.Text.Json;
using System.Text.Json.Serialization;
using Marketplace.BLL;
using Marketplace.DTOs;

namespace Marketplace.Api;

public static class RankingHttpEndpoints
{
    private static readonly JsonSerializerOptions InputJson = new(JsonSerializerDefaults.Web)
        { NumberHandling = JsonNumberHandling.Strict };

    public static void MapRankingSnapshots(this WebApplication app)
    {
        app.MapPost("/public/ranking-snapshots", Create).RequireRateLimiting("ranking-create")
            .WithMetadata(new RankingConcurrencyPolicy(true));
        app.MapGet("/public/ranking-snapshots/page", ReadPage).RequireRateLimiting("ranking-page")
            .WithMetadata(new RankingConcurrencyPolicy(false));
    }

    private static async Task<IResult> Create(HttpRequest request, HttpResponse response,
        IConfiguration config, IWebHostEnvironment environment, AuthenticationService auth,
        BusinessService businesses, RankingSnapshotService service, RankingSnapshotPages pages,
        RankingCursorTokens cursors, ActivityTokens measurement, CancellationToken ct)
    {
        response.Headers.CacheControl = "no-store";
        // Temporary local preview only. A production policy requires separate evaluated evidence.
        if (!IsPreviewEnvironment(environment) || !config.GetValue<bool>("Ranking:PreviewEnabled"))
            return Results.NotFound();
        if (config.GetValue<bool>("Ranking:WithdrawPages")) return Gone();
        if (string.IsNullOrWhiteSpace(config["FrontendOrigin"]) || request.Headers.Origin != config["FrontendOrigin"] ||
            request.Headers["X-Requested-With"] != "marketplace") return Results.StatusCode(403);
        if (!request.HasJsonContentType() || request.Query.Count != 0) return Results.BadRequest();
        var bytes = new byte[4097];
        var count = await request.Body.ReadAtLeastAsync(bytes, bytes.Length, throwOnEndOfStream: false, ct);
        if (count > 4096) return Results.StatusCode(413);
        CreateRankingSnapshotRequest? body;
        try { body = JsonSerializer.Deserialize<CreateRankingSnapshotRequest>(bytes.AsSpan(0, count), InputJson); }
        catch (JsonException) { return Results.BadRequest(); }
        if (body is null || body.RequestId == Guid.Empty || body.ZoneId is not ("centro" or "nueva-cordoba" or "alberdi") ||
            body.CategoryId <= 0 || body.RadiusKm != 4) return Results.BadRequest();
        if (!(await businesses.GetCategoriesAsync(ct)).Any(x => x.Id == body.CategoryId)) return Results.BadRequest();
        var actor = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
        SnapshotResult result;
        try
        {
            result = await service.CreateAsync(body.RequestId, body.ZoneId, body.CategoryId,
                RankingVariant.EquityOnly, new(), 17, actor?.Id, ct);
        }
        catch (RankingCapacityException)
        {
            return Results.Json(new { message = "La lista supera la capacidad disponible. Usá la guía de negocios." }, statusCode: 503);
        }
        if (result.Status == SnapshotStatus.Conflict) return Results.Conflict();
        if (result.Status == SnapshotStatus.Expired) return Gone();
        var page = await pages.ReadAsync(result.Snapshot!.Id, 0, ct);
        return page is null ? Gone() : Results.Json(PublicPage(page, 0, cursors, measurement),
            statusCode: result.Status == SnapshotStatus.Created ? 201 : 200);
    }

    private static async Task<IResult> ReadPage(HttpRequest request, HttpResponse response,
        IConfiguration config, IWebHostEnvironment environment, RankingSnapshotPages pages,
        RankingCursorTokens cursors, ActivityTokens measurement, CancellationToken ct)
    {
        response.Headers.CacheControl = "no-store";
        if (!IsPreviewEnvironment(environment)) return Results.NotFound();
        if (request.Query.Count != 1 || !request.Query.TryGetValue("cursor", out var values) || values.Count != 1)
            return Results.BadRequest();
        var result = cursors.Read(values[0]);
        if (result.Status == RankingCursorStatus.Invalid) return Results.BadRequest();
        if (result.Status == RankingCursorStatus.Expired || config.GetValue<bool>("Ranking:WithdrawPages")) return Gone();
        var cursor = result.Cursor!;
        RankingSnapshotPage? page;
        try { page = await pages.ReadAsync(cursor.SnapshotId, cursor.Offset, ct); }
        catch (ArgumentException) { return Results.BadRequest(); }
        if (page is null) return Gone();
        if (page.ExpiresAt != cursor.ExpiresAt) return Results.BadRequest();
        return Results.Ok(PublicPage(page, cursor.Offset, cursors, measurement));
    }

    private static PublicRankingPage PublicPage(RankingSnapshotPage page, int currentOffset, RankingCursorTokens cursors, ActivityTokens measurement) =>
        new(page.Items.Select(x => x.Business).ToArray(), page.ExpiresAt, page.PolicyHash, page.NextOffset.HasValue,
            page.NextOffset is { } offset ? cursors.Issue(new(page.SnapshotId, offset, page.ExpiresAt)) : null,
            page.Items.ToDictionary(x => x.Business.Id,
                x => measurement.Issue(new(x.Business.Id, "directory", x.Position, page.PolicyHash))),
            cursors.Issue(new(page.SnapshotId, currentOffset, page.ExpiresAt)));

    private static bool IsPreviewEnvironment(IWebHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("Testing");

    private static IResult Gone() => Results.Json(new { message = "La lista ya no está disponible. Actualizar resultados." }, statusCode: 410);
}
