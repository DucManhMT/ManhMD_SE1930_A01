using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface ISystemAccountRepository
{
    Task<SystemAccount?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<SystemAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<SystemAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(short id, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, short? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasCreatedArticlesAsync(short id, CancellationToken cancellationToken = default);
    Task<bool> HasUpdatedArticlesAsync(short id, CancellationToken cancellationToken = default);
    Task<SystemAccount> AddAsync(SystemAccount account, CancellationToken cancellationToken = default);
    Task<SystemAccount> UpdateAsync(SystemAccount account, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(short id, CancellationToken cancellationToken = default);
}
