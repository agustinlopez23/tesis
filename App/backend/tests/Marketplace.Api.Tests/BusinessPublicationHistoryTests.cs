using System.Net;
using System.Net.Http.Json;
using Marketplace.DAL;
using Marketplace.BLL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Text.Json;

namespace Marketplace.Api.Tests;

public sealed class BusinessPublicationHistoryTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Theory]
    [InlineData("draft")]
    [InlineData("published")]
    public async Task First_publication_survives_edits_unpublishing_and_republication(string initial)
    {
        using var client = await OwnerAsync();
        var before = DateTime.UtcNow;
        var id = await CreateAsync(client, initial);
        var state = await ReadAsync(id);
        Assert.False(state.Unknown);
        if (initial == "draft")
        {
            Assert.Null(state.First);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PutAsJsonAsync($"/businesses/{id}", Request("published", 91))).StatusCode);
            Assert.Equal(state, await ReadAsync(id));
            Assert.Equal(HttpStatusCode.OK,
                (await client.PutAsJsonAsync($"/businesses/{id}", Request("published"))).StatusCode);
            state = await ReadAsync(id);
        }
        Assert.NotNull(state.First);
        Assert.InRange(state.First.Value, before, DateTime.UtcNow);
        foreach (var status in new[] { "published", "draft", "published" })
        {
            Assert.Equal(HttpStatusCode.OK,
                (await client.PutAsJsonAsync($"/businesses/{id}", Request(status))).StatusCode);
            var updated = await ReadAsync(id);
            Assert.Equal(state.First, updated.First);
            Assert.False(updated.Unknown);
        }
    }

    [Fact]
    public async Task Stale_write_cannot_replace_first_publication()
    {
        using var client = await OwnerAsync();
        var id = await CreateAsync(client, "draft");
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var stale = await db.Businesses.SingleAsync(x => x.Id == id);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync($"/businesses/{id}", Request("published"))).StatusCode);
        var first = await ReadAsync(id);
        stale.Status = "published";
        stale.Name = "Edición atrasada";
        await db.SaveChangesAsync();
        Assert.Equal(first, await ReadAsync(id));
        Assert.Equal(first.First, stale.FirstPublishedAt?.UtcDateTime);
        Assert.False(stale.PublicationHistoryUnknown);
    }

    [Fact]
    public async Task Migration_preserves_unknown_history_for_old_published_and_draft_businesses()
    {
        using var client = await OwnerAsync();
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var migrator = db.GetService<IMigrator>();
        var ids = new List<Guid>();
        await migrator.MigrateAsync("20260919042427_AddBusinessActivity");
        try
        {
            // Raw insert uses only the old schema; HTTP uses the new model after migration.
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            foreach (var status in new[] { "draft", "published" })
            {
                await using var command = new NpgsqlCommand("""
                    INSERT INTO businesses (id, owner_id, category_id, name, description, address,
                        whatsapp, location, status, created_at, updated_at)
                    SELECT gen_random_uuid(), id, 1, 'Histórico', '', 'Córdoba', '+5493515551234',
                        ST_SetSRID(ST_MakePoint(-64.1833, -31.4167),4326)::geography, $1,
                        '2020-01-01'::timestamptz, '2020-01-01'::timestamptz
                    FROM user_accounts WHERE email = $2 RETURNING id
                    """, connection);
                command.Parameters.AddWithValue(status);
                command.Parameters.AddWithValue(client.DefaultRequestHeaders.GetValues("X-Test-Email").Single());
                ids.Add((Guid)(await command.ExecuteScalarAsync())!);
            }
        }
        finally { await migrator.MigrateAsync(); }
        foreach (var id in ids)
        {
            Assert.Equal((null, true), await ReadAsync(id));
            foreach (var status in new[] { "published", "draft", "published" })
            {
                Assert.Equal(HttpStatusCode.OK,
                    (await client.PutAsJsonAsync($"/businesses/{id}", Request(status))).StatusCode);
                Assert.Equal((null, true), await ReadAsync(id));
            }
            var repository = scope.ServiceProvider.GetRequiredService<IRankingCandidateRepository>();
            var candidate = Assert.Single(await repository.GetEligibleAsync(1,
                new DiscoveryArea(-31.4167, -64.1833, 4), default), x => x.Id == id);
            Assert.Null(candidate.FirstPublishedAt);
            Assert.True(candidate.PublicationHistoryUnknown);
        }
    }

    private async Task<(DateTime? First, bool Unknown)> ReadAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT first_published_at, publication_history_unknown FROM businesses WHERE id = $1", connection);
        command.Parameters.AddWithValue(id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.IsDBNull(0) ? null : reader.GetDateTime(0), reader.GetBoolean(1));
    }

    private async Task<HttpClient> OwnerAsync()
    {
        var client = fixture.Client();
        var email = $"history-{Guid.NewGuid():N}@example.test";
        var password = $"History-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/auth/register",
            new { email, password, displayName = "Responsable" })).StatusCode);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        return client;
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string status)
    {
        var response = await client.PostAsJsonAsync("/businesses", Request(status));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static object Request(string status, double latitude = -31.4167) => new
    {
        name = "Negocio", description = "", address = "Córdoba", categoryId = 1,
        whatsapp = "+5493515551234", latitude, longitude = -64.1833, status
    };
}
