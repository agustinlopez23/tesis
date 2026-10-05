using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Marketplace.Api;
using Marketplace.BLL;
using Marketplace.DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed partial class RankingHttpTests
{
    [Theory]
    [InlineData(true, "success")]
    [InlineData(false, "success")]
    [InlineData(true, "failure")]
    [InlineData(false, "failure")]
    [InlineData(true, "cancel")]
    [InlineData(false, "cancel")]
    public async Task Concurrency_rejects_without_queue_separates_routes_and_recovers(bool create, string outcome)
    {
        var gate = new DatabaseGate(create, 2, outcome == "failure");
        await using var factory = Preview().WithWebHostBuilder(builder => builder
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Ranking:CreateConcurrencyLimit"] = "2", ["Ranking:PageConcurrencyLimit"] = "2"
            }))
            .ConfigureServices(services => {
                services.AddHttpContextAccessor();
                services.AddDbContext<MarketplaceDbContext>(options => options.AddInterceptors(gate));
            }));
        using var client = Client(factory);
        var body = Body() with { CategoryId = 3 };
        (await client.PostAsJsonAsync(Route, body)).EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var id = await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM ranking_snapshots WHERE request_id = {body.RequestId}").SingleAsync();
        var snapshot = (await scope.ServiceProvider.GetRequiredService<IRankingSnapshotStore>().FindAsync(id, default))!;
        var cursor = scope.ServiceProvider.GetRequiredService<RankingCursorTokens>().Issue(new(id, 0, snapshot.ExpiresAt));
        var pageUrl = Route + "/page?cursor=" + Uri.EscapeDataString(cursor);
        Task<HttpResponseMessage> Send(bool post, CancellationToken ct = default) => post
            ? client.PostAsJsonAsync(Route, body with { RequestId = Guid.NewGuid() }, ct)
            : client.GetAsync(pageUrl, ct);
        using var cancellation = new CancellationTokenSource();
        gate.Armed = true;
        var pending = new[] { Send(create, cancellation.Token), Send(create, cancellation.Token) };
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using var otherClient = Client(factory);
            var rejected = await (create
                ? otherClient.PostAsJsonAsync(Route, body)
                : otherClient.GetAsync(pageUrl)).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            AssertNoStore(rejected);
            // A saturated creation pool cannot consume page permits, or vice versa.
            (await Send(!create).WaitAsync(TimeSpan.FromSeconds(3))).EnsureSuccessStatusCode();
        }
        finally
        {
            gate.Armed = false;
            if (outcome == "cancel") cancellation.Cancel();
            gate.Release.TrySetResult();
            try { await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) when (outcome == "cancel") { }
        }
        if (outcome == "cancel") Assert.All(pending, task => Assert.True(task.IsCanceled));
        else Assert.All(pending, task => {
            Assert.Equal(outcome == "failure" ? HttpStatusCode.InternalServerError :
                create ? HttpStatusCode.Created : HttpStatusCode.OK, task.Result.StatusCode);
            AssertNoStore(task.Result);
        });
        // Client cancellation can finish before the server's finally block releases its lease.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        HttpResponseMessage recovered;
        do
        {
            recovered = await Send(create, deadline.Token);
            if (recovered.StatusCode != HttpStatusCode.TooManyRequests) break;
            recovered.Dispose();
            await Task.Delay(25, deadline.Token);
        } while (true);
        recovered.EnsureSuccessStatusCode();
        AssertNoStore(recovered);
    }

    [Theory]
    [InlineData("MaxCandidates", "0")]
    [InlineData("MaxCandidates", "10001")]
    [InlineData("MaxBalanceRows", "0")]
    [InlineData("MaxBalanceRows", "100001")]
    [InlineData("CreateConcurrencyLimit", "0")]
    [InlineData("CreateConcurrencyLimit", "65")]
    [InlineData("PageConcurrencyLimit", "0")]
    [InlineData("PageConcurrencyLimit", "65")]
    public async Task Invalid_capacity_configuration_fails_at_startup(string key, string value)
    {
        await using var factory = Preview().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Ranking:" + key] = value })));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateClient());
    }

    private sealed class DatabaseGate(bool create, int expected, bool fail) : DbCommandInterceptor
    {
        public bool Armed { get; set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int entered;

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            var method = new HttpContextAccessor().HttpContext?.Request.Method;
            var matches = create
                ? method == "POST" && command.CommandText.Contains("FROM ranking_snapshots WHERE request_id")
                : method == "GET" && command.CommandText.Contains("FROM ranking_snapshots WHERE id");
            if (Armed && matches)
            {
                if (Interlocked.Increment(ref entered) == expected) Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
                if (fail) throw new InvalidOperationException("Injected database boundary failure.");
            }
            return result;
        }
    }
}
