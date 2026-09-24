using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IAccountClientService
{
    Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<AccountApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default);
    Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default);
    Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default);
}
