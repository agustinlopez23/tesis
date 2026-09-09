namespace Marketplace.BLL;

public interface IDatabaseInitializer
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
