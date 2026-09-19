using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.IoC;

public static class MarketplaceServices
{
    public static IServiceCollection AddMarketplace(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MarketplaceDbContext>((provider, options) =>
        {
            var connection = provider.GetRequiredService<IConfiguration>().GetConnectionString("Marketplace")
                ?? throw new InvalidOperationException("ConnectionStrings:Marketplace is required.");
            options.UseNpgsql(connection, x => x.UseNetTopologySuite());
        });
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<BusinessService>();
        services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();
        services.AddScoped<IPhotoProcessor, PhotoProcessorAdapter>();
        services.AddScoped<BusinessProfileService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ActivityService>();
        services.AddScoped<IPasswordService, AspNetPasswordService>();
        return services;
    }
}

internal sealed class AspNetPasswordService : IPasswordService
{
    private readonly PasswordHasher<UserAccount> hasher = new();
    public string Hash(UserAccount account, string password) => hasher.HashPassword(account, password);
    public PasswordStatus Verify(UserAccount account, string password) =>
        hasher.VerifyHashedPassword(account, account.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordStatus.Valid,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordStatus.NeedsRehash,
            _ => PasswordStatus.Invalid
        };
}
