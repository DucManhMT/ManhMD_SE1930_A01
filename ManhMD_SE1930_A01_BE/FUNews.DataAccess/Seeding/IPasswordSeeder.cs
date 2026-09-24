namespace FUNews.DataAccess.Seeding;

public interface IPasswordSeeder
{
    Task<int> SeedLegacyPasswordsAsync(CancellationToken cancellationToken = default);
}
