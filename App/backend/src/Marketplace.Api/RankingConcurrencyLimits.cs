using System.Threading.RateLimiting;

namespace Marketplace.Api;

public sealed class RankingConcurrencyLimits : IDisposable
{
    public ConcurrencyLimiter Create { get; }
    public ConcurrencyLimiter Page { get; }

    public RankingConcurrencyLimits(IConfiguration configuration)
    {
        var create = configuration.GetValue("Ranking:CreateConcurrencyLimit", 4);
        var page = configuration.GetValue("Ranking:PageConcurrencyLimit", 16);
        if (create is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(create));
        if (page is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(page));
        Create = new(new() { PermitLimit = create, QueueLimit = 0 });
        Page = new(new() { PermitLimit = page, QueueLimit = 0 });
    }

    public void Dispose()
    {
        Create.Dispose();
        Page.Dispose();
    }
}

public sealed record RankingConcurrencyPolicy(bool IsCreation);

public sealed class RankingConcurrencyMiddleware(RequestDelegate next, RankingConcurrencyLimits limits)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var policy = context.GetEndpoint()?.Metadata.GetMetadata<RankingConcurrencyPolicy>();
        if (policy is null)
        {
            await next(context);
            return;
        }
        // Keep the permit through response serialization, and release on failure/cancellation too.
        using var lease = (policy.IsCreation ? limits.Create : limits.Page).AttemptAcquire();
        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }
        await next(context);
    }
}
