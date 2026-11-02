using System.Xml.Linq;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Api;

// Data Protection key ring in PostgreSQL: containers have no durable disk, and every restart or
// redeploy would otherwise invalidate activity, campaign and ranking-cursor tokens.
public sealed class DatabaseXmlRepository(IServiceScopeFactory scopes) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = scopes.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        return database.DataProtectionKeys.AsNoTracking().OrderBy(key => key.Id).Select(key => key.Xml).ToList()
            .Select(XElement.Parse).ToList().AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = scopes.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        database.DataProtectionKeys.Add(new DataProtectionKeyRecord
        {
            FriendlyName = friendlyName,
            Xml = element.ToString(SaveOptions.DisableFormatting)
        });
        database.SaveChanges();
    }
}
