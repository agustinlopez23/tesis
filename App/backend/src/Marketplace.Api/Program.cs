using Marketplace.BLL;
using Marketplace.Api;
using Marketplace.DTOs;
using Marketplace.IoC;
using Marketplace.Models;
using Microsoft.AspNetCore.Diagnostics;
using System.Data.Common;
using System.Threading.RateLimiting;

const string SessionCookie = "marketplace_session";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMarketplace(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<ActivityTokens>();
builder.Services.AddSingleton<RankingCursorTokens>();
builder.Services.AddSingleton<RankingConcurrencyLimits>();
builder.Services.AddHostedService<ActivityRetentionWorker>();
builder.Services.AddHostedService<RankingRetentionWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ranking-create", _ => RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions {
        PermitLimit = builder.Configuration.GetValue("Ranking:CreatePermitLimit", 60),
        Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
    }));
    options.AddPolicy("ranking-page", _ => RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions {
        PermitLimit = builder.Configuration.GetValue("Ranking:PagePermitLimit", 600),
        Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
    }));
    options.AddPolicy("activity", _ => RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions {
        PermitLimit = 1200, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
    }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("Auth:PermitLimit", 5),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
var app = builder.Build();
// Reject invalid capacity settings before accepting traffic, even with preview disabled.
_ = app.Services.GetRequiredService<RankingReadLimits>();
_ = app.Services.GetRequiredService<RankingConcurrencyLimits>();
app.UseExceptionHandler(errorApplication => errorApplication.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var databaseFailure = IsDatabaseFailure(error);
    context.Response.StatusCode = databaseFailure
        ? StatusCodes.Status503ServiceUnavailable
        : StatusCodes.Status500InternalServerError;
    await Results.Problem(
        statusCode: context.Response.StatusCode,
        title: databaseFailure ? "Servicio de datos no disponible" : "Error inesperado",
        detail: databaseFailure ? "Intentá nuevamente en unos momentos." : "No pudimos completar la operación.")
        .ExecuteAsync(context);
}));
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/public/ranking-snapshots"))
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
    await next(context);
});
app.UseRateLimiter();
app.UseMiddleware<RankingConcurrencyMiddleware>();
app.MapBusinessProfiles();
app.MapCatalog();
app.MapBusinessActivity();
app.MapRankingSnapshots();

app.MapGet("/health/live", () => Results.Json(new { status = "ok" }));

app.MapGet("/public/businesses", async (
    int? page, string? q, string? category, double? lat, double? lon, double? radiusKm,
    HttpResponse response, BusinessService businesses, ActivityTokens tokens, CancellationToken cancellationToken) =>
{
    response.Headers.CacheControl = "no-store";
    var currentPage = page ?? 1;
    if (currentPage is < 1 or > 10000)
        return Results.BadRequest(new { message = "La página debe estar entre 1 y 10000." });
    if (q?.Length > 150 || category?.Length > 100)
        return Results.BadRequest(new { message = "La búsqueda es demasiado larga." });
    DiscoveryArea? area = null;
    if (lat.HasValue || lon.HasValue || radiusKm.HasValue)
    {
        if (lat is null || lon is null || radiusKm is null)
            return Results.BadRequest(new { message = "Indicá latitud, longitud y radio juntos." });
        area = new DiscoveryArea(lat.Value, lon.Value, radiusKm.Value);
        if (!area.IsValid)
            return Results.BadRequest(new { message = "La ubicación debe ser válida y el radio debe estar entre 0,1 y 50 km." });
    }
    var result = await businesses.GetPublishedAsync(currentPage, q, category, area, cancellationToken);
    return Results.Ok(result with { Measurement = result.Items.Select((item, index) => new {
        item.Id, Token = tokens.Issue(new(item.Id, string.IsNullOrWhiteSpace(q) ? "directory" : "search",
            (currentPage - 1) * 20 + index + 1, area is null ? "name-v1" : "distance-v1"))
    }).ToDictionary(x => x.Id, x => x.Token) });
});

app.MapPost("/auth/register", async (
    HttpRequest httpRequest,
    RegisterOwnerRequest request,
    AuthenticationService authentication,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsTrustedMutation(httpRequest, configuration)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var result = await authentication.RegisterAsync(request, cancellationToken);
    return result.Status switch
    {
        RegistrationStatus.Created => Results.Json(result.Owner, statusCode: StatusCodes.Status201Created),
        RegistrationStatus.Duplicate => Results.Conflict(new { message = "El correo ya está registrado." }),
        _ => Results.BadRequest(new { message = "Revisá los datos ingresados." })
    };
}).RequireRateLimiting("auth");

app.MapPost("/auth/login", async (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    LoginRequest request,
    AuthenticationService authentication,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (!IsTrustedMutation(httpRequest, configuration)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var result = await authentication.LoginAsync(request, cancellationToken);
    if (result is null) return Results.Unauthorized();
    httpResponse.Cookies.Append(SessionCookie, result.Token, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
        Path = "/",
        Expires = result.ExpiresAt
    });
    return Results.Ok(result.Owner);
}).RequireRateLimiting("auth");

app.MapGet("/auth/session", async (
    HttpRequest request,
    AuthenticationService authentication,
    CancellationToken cancellationToken) =>
{
    var owner = await authentication.GetSessionAsync(request.Cookies[SessionCookie], cancellationToken);
    return owner is null ? Results.Unauthorized() : Results.Ok(owner);
});

app.MapPost("/auth/logout", async (
    HttpRequest request,
    HttpResponse response,
    AuthenticationService authentication,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsTrustedMutation(request, configuration)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    await authentication.LogoutAsync(request.Cookies[SessionCookie], cancellationToken);
    response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/" });
    return Results.NoContent();
});

app.MapGet("/business-categories", async (BusinessService businesses, CancellationToken cancellationToken) =>
    Results.Ok(await businesses.GetCategoriesAsync(cancellationToken)));

app.MapGet("/businesses", async (
    HttpRequest request,
    AuthenticationService authentication,
    BusinessService businesses,
    CancellationToken cancellationToken) =>
{
    var owner = await CurrentOwnerAsync(request, authentication, cancellationToken);
    return owner is null
        ? Results.Unauthorized()
        : Results.Ok(await businesses.GetOwnedAsync(owner.Id, cancellationToken));
});

app.MapPost("/businesses", async (
    HttpRequest request,
    SaveBusinessRequest body,
    AuthenticationService authentication,
    BusinessService businesses,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsTrustedMutation(request, configuration)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var owner = await CurrentOwnerAsync(request, authentication, cancellationToken);
    if (owner is null) return Results.Unauthorized();
    var result = await businesses.CreateAsync(owner.Id, body, cancellationToken);
    return result.Status == BusinessSaveStatus.Saved
        ? Results.Json(result.Business, statusCode: StatusCodes.Status201Created)
        : Results.BadRequest(new { message = "Revisá los datos del negocio." });
});

app.MapGet("/businesses/{id:guid}", async (
    Guid id,
    HttpRequest request,
    AuthenticationService authentication,
    BusinessService businesses,
    CancellationToken cancellationToken) =>
{
    var owner = await CurrentOwnerAsync(request, authentication, cancellationToken);
    if (owner is null) return Results.Unauthorized();
    var business = await businesses.GetOwnedAsync(owner.Id, id, cancellationToken);
    return business is null ? Results.NotFound() : Results.Ok(business);
});

app.MapPut("/businesses/{id:guid}", async (
    Guid id,
    HttpRequest request,
    SaveBusinessRequest body,
    AuthenticationService authentication,
    BusinessService businesses,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsTrustedMutation(request, configuration)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var owner = await CurrentOwnerAsync(request, authentication, cancellationToken);
    if (owner is null) return Results.Unauthorized();
    var result = await businesses.UpdateAsync(owner.Id, id, body, cancellationToken);
    return result.Status switch
    {
        BusinessSaveStatus.Saved => Results.Ok(result.Business),
        BusinessSaveStatus.NotFound => Results.NotFound(),
        _ => Results.BadRequest(new { message = "Revisá los datos del negocio." })
    };
});

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().MigrateAsync();
    return;
}

app.Run();

static bool IsTrustedMutation(HttpRequest request, IConfiguration configuration) =>
    request.Headers.Origin == configuration["FrontendOrigin"] &&
    request.Headers["X-Requested-With"] == "marketplace";

static Task<OwnerIdentityResponse?> CurrentOwnerAsync(
    HttpRequest request,
    AuthenticationService authentication,
    CancellationToken cancellationToken) =>
    authentication.GetSessionAsync(request.Cookies[SessionCookie], cancellationToken);

static bool IsDatabaseFailure(Exception? exception)
{
    while (exception is not null)
    {
        if (exception is DbException) return true;
        exception = exception.InnerException;
    }
    return false;
}

public partial class Program { }
