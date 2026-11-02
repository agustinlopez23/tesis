namespace Marketplace.BLL;

// A tracked balance strictly below the threshold (and above zero) is shown as low availability.
public sealed record CatalogSettings(int LowStockThreshold)
{
    public bool IsLow(int? quantity) => quantity is > 0 && quantity < LowStockThreshold;
}
