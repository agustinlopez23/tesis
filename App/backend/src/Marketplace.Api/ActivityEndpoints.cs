using System.Text.Json;
using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.Api;

public static class ActivityEndpoints
{
    public static void MapBusinessActivity(this WebApplication app)
    {
        app.MapPost("/public/events", async (HttpRequest request, HttpResponse response, IConfiguration config,
            ActivityTokens tokens, AuthenticationService auth, ActivityService activity, TimeProvider clock, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            if (request.Headers.Origin != config["FrontendOrigin"] || request.Headers["X-Requested-With"] != "marketplace")
                return Results.StatusCode(403);
            if (!request.HasJsonContentType()) return Results.BadRequest();
            // Bound chunked requests as well as requests with Content-Length before deserializing.
            var bytes = new byte[4097];
            var count = await request.Body.ReadAtLeastAsync(bytes, bytes.Length, throwOnEndOfStream: false, ct);
            if (count > 4096) return Results.StatusCode(413);
            RecordActivityRequest? body;
            try { body = JsonSerializer.Deserialize<RecordActivityRequest>(bytes.AsSpan(0, count), JsonSerializerOptions.Web); }
            catch (JsonException) { return Results.BadRequest(); }
            var context = tokens.Read(body?.Token);
            if (body is null || context is null || body.VisitId == Guid.Empty ||
                body.Kind is not ("impression" or "profile_view" or "contact") || body.Surface is not ("directory" or "profile") ||
                (body.Kind == "impression" && body.Surface != "directory") ||
                (body.Kind == "profile_view" && body.Surface != "profile") ||
                (body.Surface == "directory" && context.Position is null)) return Results.BadRequest();
            var actor = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
            await activity.RecordAsync(new BusinessActivity {
                BusinessId = context.BusinessId, VisitId = body.VisitId, Kind = body.Kind, Surface = body.Surface,
                Source = context.Source, Position = context.Position, RankingVersion = context.RankingVersion,
                OccurredAt = clock.GetUtcNow()
            }, actor?.Id, ct);
            return Results.NoContent();
        }).RequireRateLimiting("activity");

        app.MapGet("/businesses/{id:guid}/activity", async (Guid id, int? days, HttpRequest request,
            HttpResponse response, AuthenticationService auth, ActivityService activity, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            var owner = await auth.GetSessionAsync(request.Cookies["marketplace_session"], ct);
            if (owner is null) return Results.Unauthorized();
            if (days is not (null or 7 or 30)) return Results.BadRequest();
            var report = await activity.ReadAsync(id, owner.Id, days ?? 30, ct);
            return report is null ? Results.NotFound() : Results.Ok(report);
        });
    }
}
