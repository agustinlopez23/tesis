namespace Marketplace.Models;

// One ASP.NET Core Data Protection key, stored as XML so tokens survive restarts and redeploys.
public sealed class DataProtectionKeyRecord
{
    public int Id { get; set; }
    public string? FriendlyName { get; set; }
    public string Xml { get; set; } = "";
}
