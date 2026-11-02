using Marketplace.BLL;

namespace Marketplace.Api;

// Records a substantive owner update after a successful write (HU-22). The handler has already
// authorized the owner; refused or failed writes never count. Recording never fails the write.
public static class OwnerActivityFilter
{
    public static TBuilder RecordsOwnerActivity<TBuilder>(this TBuilder builder, string kind, string routeKey = "id")
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            var status = result is IStatusCodeHttpResult code ? code.StatusCode ?? StatusCodes.Status200OK : (int?)null;
            if (status is >= 200 and < 300 &&
                Guid.TryParse(context.HttpContext.GetRouteValue(routeKey)?.ToString(), out var business))
            {
                var http = context.HttpContext;
                try { await http.RequestServices.GetRequiredService<OwnerActivityService>().RecordAsync(business, kind, http.RequestAborted); }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    http.RequestServices.GetRequiredService<ILogger<OwnerActivityService>>()
                        .LogWarning(error, "Owner activity could not be recorded for {Kind}.", kind);
                }
            }
            return result;
        });
}
