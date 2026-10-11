using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public class SystemAccountRepository : ISystemAccountRepository
{
    private readonly SystemAccountDAO _accountDao;

    public SystemAccountRepository(SystemAccountDAO accountDao)
    {
        _accountDao = accountDao ?? throw new ArgumentNullException(nameof(accountDao));
    }

    public IQueryable<SystemAccount> GetQueryable()
    {
        return _accountDao.GetQueryable();
    }

    public Task<SystemAccount?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return _accountDao.GetByIdAsync(id, cancellationToken);
    }

    public Task<SystemAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _accountDao.GetByEmailAsync(email, cancellationToken);
    }

    public Task<List<SystemAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _accountDao.GetAllAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(short id, CancellationToken cancellationToken = default)
    {
        return _accountDao.ExistsAsync(id, cancellationToken);
    }

    public Task<bool> IsEmailUniqueAsync(string email, short? excludeId = null, CancellationToken cancellationToken = default)
    {
        return _accountDao.IsEmailUniqueAsync(email, excludeId, cancellationToken);
    }

    public Task<bool> HasCreatedArticlesAsync(short id, CancellationToken cancellationToken = default)
    {
        return _accountDao.HasCreatedArticlesAsync(id, cancellationToken);
    }

    public Task<bool> HasUpdatedArticlesAsync(short id, CancellationToken cancellationToken = default)
    {
        return _accountDao.HasUpdatedArticlesAsync(id, cancellationToken);
    }

    public Task<SystemAccount> AddAsync(SystemAccount account, CancellationToken cancellationToken = default)
    {
        return _accountDao.AddAsync(account, cancellationToken);
    }

    public Task<SystemAccount> UpdateAsync(SystemAccount account, CancellationToken cancellationToken = default)
    {
        return _accountDao.UpdateAsync(account, cancellationToken);
    }

    public Task<bool> DeleteAsync(short id, CancellationToken cancellationToken = default)
    {
        return _accountDao.DeleteAsync(id, cancellationToken);
    }
}
