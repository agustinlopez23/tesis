using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

public interface IProductDiscoveryRepository
{
    Task<ProductDiscoveryResponse> Read(int page, string query, string category, DiscoveryArea? area, CancellationToken ct);
}
public sealed class ProductDiscoveryService(IProductDiscoveryRepository repository)
{
    public Task<ProductDiscoveryResponse> Read(int page, string? query, string? category, DiscoveryArea? area, CancellationToken ct) =>
        repository.Read(page, (query ?? "").Trim(), (category ?? "").Trim(), area, ct);
}
