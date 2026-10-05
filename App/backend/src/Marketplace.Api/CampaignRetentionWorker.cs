using Marketplace.BLL;

namespace Marketplace.Api;

public sealed class CampaignRetentionWorker(IServiceScopeFactory scopes, IHostEnvironment environment, ILogger<CampaignRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try {
            do {
                try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<CampaignService>().Purge(stoppingToken); }
                catch (Exception error) when (error is not OperationCanceledException) { logger.LogError(error, "Campaign event retention failed; retry on next cycle"); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
