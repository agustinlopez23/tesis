using System.Threading.Channels;
using Marketplace.Api;
using Marketplace.BLL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Api.Tests;

public sealed class RankingRetentionWorkerTests
{
    [Fact]
    public async Task Cleanup_runs_at_startup_and_retries_next_hour_after_failure()
    {
        var store = new Store();
        using var provider = new ServiceCollection().AddScoped<IRankingSnapshotStore>(_ => store).BuildServiceProvider();
        var clock = new Clock();
        using var worker = new RankingRetentionWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            new Environment(), NullLogger<RankingRetentionWorker>.Instance, clock);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(timeout.Token);
        Assert.Equal(1, await store.Attempts.Reader.ReadAsync(timeout.Token));
        Assert.Equal(TimeSpan.FromHours(1), clock.Period);
        clock.Tick();
        Assert.Equal(2, await store.Attempts.Reader.ReadAsync(timeout.Token));
        await worker.StopAsync(timeout.Token);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    private sealed class Store : IRankingSnapshotStore
    {
        public Channel<int> Attempts { get; } = Channel.CreateUnbounded<int>();
        private int count;
        public Task PurgeAsync(CancellationToken ct)
        {
            var attempt = Interlocked.Increment(ref count);
            Attempts.Writer.TryWrite(attempt);
            if (attempt == 1) throw new InvalidOperationException("temporary storage failure");
            return Task.CompletedTask;
        }
        public Task<SnapshotResult> CreateAsync(Guid id, string key, Func<long, CancellationToken, Task<RankingOpportunity>> build, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, ExposureBalance>> ReadBalancesAsync(string key, DateTimeOffset at, CancellationToken ct) => throw new NotSupportedException();
        public Task<RankingSnapshot?> FindAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Clock : TimeProvider
    {
        private Action tick = () => { };
        public TimeSpan Period { get; private set; }
        public void Tick() => tick();
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Period = period;
            tick = () => callback(state);
            return new Timer();
        }
        private sealed class Timer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
    private sealed class Environment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
