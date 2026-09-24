using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;

namespace FUNews.BusinessLogic.Services;

public interface IAccountService
{
    IQueryable<AccountDto> GetQueryable();
    Task<AccountDto?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<AccountDto> CreateAsync(CreateAccountRequestDto request, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateAsync(short id, UpdateAccountRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(short id, CancellationToken cancellationToken = default);
}
