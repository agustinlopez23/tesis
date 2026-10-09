using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.Api;
using Marketplace.BLL;
using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed partial class RankingHttpTests
{
    [Fact]
    public async Task Separate_global_quotas_reject_without_queue_and_always_disable_caching()
    {
        await using var factory = Preview().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Ranking:CreatePermitLimit"] = "1", ["Ranking:PagePermitLimit"] = "1"
            })));
        using var client = Client(factory);
        (await client.PostAsJsonAsync(Route, Body() with { CategoryId = 3 })).EnsureSuccessStatusCode();
        using var anotherClient = Client(factory);
        var limited = await anotherClient.PostAsJsonAsync(Route, Body());
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        AssertNoStore(limited);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + "/page?cursor=bad")).StatusCode);
        limited = await anotherClient.GetAsync(Route + "/page?cursor=bad");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        AssertNoStore(limited);
    }

    [Fact]
    public async Task Disabling_creation_preserves_live_pages_and_emergency_withdrawal_closes_both()
    {
        await using var factory = Preview();
        using var client = Client(factory);
        var body = Body() with { CategoryId = 3 };
        var response = await client.PostAsJsonAsync(Route, body);
        response.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var id = await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM ranking_snapshots WHERE request_id = {body.RequestId}").SingleAsync();
        var snapshot = (await scope.ServiceProvider.GetRequiredService<IRankingSnapshotStore>().FindAsync(id, default))!;
        var tokens = scope.ServiceProvider.GetRequiredService<RankingCursorTokens>();
        var url = Route + "/page?cursor=" + tokens.Issue(new(id, 0, snapshot.ExpiresAt));
        var config = factory.Services.GetRequiredService<IConfiguration>();
        config["Ranking:PreviewEnabled"] = "false";
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Route, Body())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        var badOffset = tokens.Issue(new(id, 20, snapshot.ExpiresAt));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + "/page?cursor=" + badOffset)).StatusCode);
        var wrongExpiry = tokens.Issue(new(id, 0, snapshot.ExpiresAt.AddMinutes(1)));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Route + "/page?cursor=" + wrongExpiry)).StatusCode);
        config["Ranking:PreviewEnabled"] = "true";
        config["Ranking:WithdrawPages"] = "true";
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(Route, Body())).StatusCode);
    }

    [Fact]
    public async Task Authenticated_owner_preview_assigns_zero_without_exposing_identity()
    {
        await using var factory = Preview();
        using var client = Client(factory);
        var email = $"ranking-owner-{Guid.NewGuid():N}@example.test";
        (await client.PostAsJsonAsync("/auth/register", new { displayName = "Dueño", email, password = "UnaClaveSegura123!" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = "UnaClaveSegura123!" });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", login.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        (await client.PostAsJsonAsync("/businesses", new { name = "Propio", description = "", address = "Córdoba", categoryId = 2,
            whatsApp = "+5493515551234", latitude = -31.416281, longitude = -64.184022, status = "published" })).EnsureSuccessStatusCode();
        var body = Body() with { CategoryId = 2 };
        var response = await client.PostAsJsonAsync(Route, body);
        response.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var json = await db.Database.SqlQuery<string>($"SELECT o.payload::text AS \"Value\" FROM ranking_opportunities o JOIN ranking_snapshots s ON o.id = s.id WHERE s.request_id = {body.RequestId}").SingleAsync();
        var opportunity = JsonSerializer.Deserialize<RankingOpportunity>(json)!;
        Assert.Equal("own-business-preview", opportunity.Reason);
        Assert.NotEmpty(opportunity.Candidates);
        Assert.All(opportunity.Candidates, row => { Assert.Equal(0, row.Expected); Assert.Equal(0, row.Received); });
        Assert.DoesNotContain(email, await response.Content.ReadAsStringAsync());
    }
}
