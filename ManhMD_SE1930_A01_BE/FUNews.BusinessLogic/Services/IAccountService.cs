using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Services;

public interface IAccountService
{
    IQueryable<AccountDto> GetQueryable();
    Task<AccountDto?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
}
