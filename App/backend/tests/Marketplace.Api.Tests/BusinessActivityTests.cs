using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.Api;
using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Marketplace.Api.Tests;

public sealed class BusinessActivityTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Daily_buckets_use_Cordoba_midnight_and_keep_zero_days()
    {
        using var owner = await Owner();
        var id = await Create(owner);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var business = await db.Businesses.SingleAsync(x => x.Id == id);
        var now = DateTimeOffset.UtcNow;
        var today = now.ToOffset(TimeSpan.FromHours(-3)).Date;
        var midnight = new DateTimeOffset(today, TimeSpan.FromHours(-3)).ToUniversalTime();
        db.BusinessActivity.AddRange(
            new BusinessActivity { VisitId = Guid.NewGuid(), BusinessId = id, Kind = "profile_view", Surface = "profile", Source = "unknown", OccurredAt = midnight.AddSeconds(-1) },
            new BusinessActivity { VisitId = Guid.NewGuid(), BusinessId = id, Kind = "profile_view", Surface = "profile", Source = "unknown", OccurredAt = midnight });
        await db.SaveChangesAsync();
        var report = await scope.ServiceProvider.GetRequiredService<ActivityService>().ReadAsync(id, business.OwnerId, 7, CancellationToken.None);
        Assert.NotNull(report);
        Assert.Equal(7, report.Daily.Count);
        Assert.Equal(1, report.Daily[^2].ProfileViews);
        Assert.Equal(1, report.Daily[^1].ProfileViews);
        Assert.All(report.Daily.Take(5), day => Assert.Equal(0, day.ProfileViews));
    }
    [Fact]
    public async Task Retention_excludes_expired_rows_and_purges_them_without_removing_recent_activity()
    {
        using var owner = await Owner();
        var id = await Create(owner);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var oldId = Guid.NewGuid();
        db.BusinessActivity.AddRange(
            new BusinessActivity { VisitId = oldId, BusinessId = id, Kind = "profile_view", Surface = "profile", Source = "unknown", OccurredAt = DateTimeOffset.UtcNow.AddDays(-31) },
            new BusinessActivity { VisitId = Guid.NewGuid(), BusinessId = id, Kind = "contact", Surface = "profile", Source = "unknown", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();
        var report = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/activity?days=30");
        Assert.Equal(0, report.GetProperty("profileViews").GetInt32());
        Assert.Equal(1, report.GetProperty("contacts").GetInt32());
        // The cleanup must also be wired into the application lifecycle, not just callable manually.
        Assert.Contains(fixture.Factory.Services.GetServices<IHostedService>(), service => service.GetType().Name == "ActivityRetentionWorker");
        await scope.ServiceProvider.GetRequiredService<ActivityService>().PurgeAsync(CancellationToken.None);
        Assert.False(await db.BusinessActivity.AnyAsync(x => x.VisitId == oldId));
        Assert.Equal(1, await db.BusinessActivity.CountAsync(x => x.BusinessId == id));
    }

    [Fact]
    public async Task Position_origin_and_version_come_from_the_served_list_and_expired_tokens_are_rejected()
    {
        using var owner = await Owner();
        using var visitor = fixture.Client();
        var id = await Create(owner);
        var page = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=Actividad&lat=-31.4&lon=-64.18&radiusKm=2");
        var token = page.GetProperty("measurement").GetProperty(id.ToString()).GetString();
        var expectedPosition = page.GetProperty("items").EnumerateArray().ToList().FindIndex(x => x.GetProperty("id").GetGuid() == id) + 1;
        var visitId = Guid.NewGuid();
        (await visitor.PostAsJsonAsync("/public/events", new { token, visitId, kind = "impression", surface = "directory", position = 999, rankingVersion = "forged" })).EnsureSuccessStatusCode();
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var entry = await db.BusinessActivity.SingleAsync(x => x.VisitId == visitId);
        Assert.Equal(expectedPosition, entry.Position);
        Assert.Equal("distance-v1", entry.RankingVersion);
        Assert.Equal("search", entry.Source);
        var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("Cerca.Activity.v1").ToTimeLimitedDataProtector();
        var expired = protector.Protect(JsonSerializer.Serialize(new ActivityContext(id, "directory", 1, "name-v1")), DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.PostAsJsonAsync("/public/events", new { token = expired, visitId = Guid.NewGuid(), kind = "impression", surface = "directory" })).StatusCode);
        var business = await db.Businesses.SingleAsync(x => x.Id == id);
        var account = await db.UserAccounts.SingleAsync(x => x.Id == business.OwnerId);
        account.IsSuspended = true;
        await db.SaveChangesAsync();
        (await visitor.PostAsJsonAsync("/public/events", new { token, visitId = Guid.NewGuid(), kind = "impression", surface = "directory" })).EnsureSuccessStatusCode();
        Assert.Equal(1, await db.BusinessActivity.CountAsync(x => x.BusinessId == id));
    }
    [Fact]
    public async Task Visible_events_are_idempotent_private_and_exclude_the_owner()
    {
        using var owner = await Owner();
        using var other = await Owner();
        using var visitor = fixture.Client();
        var id = await Create(owner);
        var token = await DirectoryToken(visitor, id);
        var visitId = Guid.NewGuid();
        var impression = new { token, visitId, kind = "impression", surface = "directory" };
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => visitor.PostAsJsonAsync("/public/events", impression)));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NoContent, r.StatusCode));
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync("/public/events", impression with { visitId = Guid.NewGuid() })).StatusCode);
        var profile = await visitor.GetFromJsonAsync<JsonElement>($"/public/businesses/{id}");
        var profileToken = profile.GetProperty("measurementToken").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync("/public/events", new { token = profileToken, visitId, kind = "profile_view", surface = "profile" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync("/public/events", new { token = profileToken, visitId, kind = "contact", surface = "profile" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync($"/businesses/{id}/activity")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/businesses/{id}/activity")).StatusCode);
        var report = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/activity?days=7");
        Assert.Equal(1, report.GetProperty("impressions").GetInt32());
        Assert.Equal(1, report.GetProperty("profileViews").GetInt32());
        Assert.Equal(1, report.GetProperty("contacts").GetInt32());
        Assert.Equal(7, report.GetProperty("daily").GetArrayLength());
        Assert.Equal(1, report.GetProperty("daily").EnumerateArray().Sum(x => x.GetProperty("impressions").GetInt32()));
        Assert.Equal("America/Argentina/Cordoba", report.GetProperty("timeZone").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/businesses/{id}/activity?days=90")).StatusCode);
    }

    [Fact]
    public async Task Ingestion_rejects_forged_invalid_untrusted_and_unpublished_events()
    {
        using var owner = await Owner();
        using var visitor = fixture.Client();
        using var untrusted = fixture.Client(false);
        var id = await Create(owner);
        var token = await DirectoryToken(visitor, id);
        var entry = new { token, visitId = Guid.NewGuid(), kind = "impression", surface = "directory" };
        Assert.Equal(HttpStatusCode.Forbidden, (await untrusted.PostAsJsonAsync("/public/events", entry)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.PostAsJsonAsync("/public/events", entry with { token = "forged" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.PostAsJsonAsync("/public/events", entry with { kind = "sale" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.PostAsJsonAsync("/public/events", entry with { visitId = Guid.Empty })).StatusCode);
        var profile = await visitor.GetFromJsonAsync<JsonElement>($"/public/businesses/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.PostAsJsonAsync("/public/events", entry with { token = profile.GetProperty("measurementToken").GetString()! })).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await visitor.PostAsJsonAsync("/public/events", entry with { token = new string('x', 5000) })).StatusCode);
        (await owner.PutAsJsonAsync($"/businesses/{id}", Business("draft"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync("/public/events", entry)).StatusCode);
        var report = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/activity");
        Assert.Equal(0, report.GetProperty("impressions").GetInt32());
    }

    private async Task<HttpClient> Owner()
    {
        var client = fixture.Client();
        var email = $"activity-{Guid.NewGuid():N}@example.test";
        const string password = "Activity-test-password-123";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Actividad" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        return client;
    }
    private static async Task<Guid> Create(HttpClient owner)
    {
        var result = await owner.PostAsJsonAsync("/businesses", Business("published"));
        result.EnsureSuccessStatusCode();
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static async Task<string> DirectoryToken(HttpClient client, Guid id)
    {
        var page = await client.GetFromJsonAsync<JsonElement>("/public/businesses?q=Actividad");
        return page.GetProperty("measurement").GetProperty(id.ToString()).GetString()!;
    }
    private static object Business(string status) => new { name = "Actividad", description = "Negocio de prueba",
        address = "Córdoba", categoryId = 1, whatsapp = "+5493515551234", latitude = -31.4, longitude = -64.18, status };
}
