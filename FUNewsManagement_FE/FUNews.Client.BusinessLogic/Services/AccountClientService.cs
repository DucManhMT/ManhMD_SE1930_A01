using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class AccountClientService : IAccountClientService
{
    private readonly IFUNewsApiClient _apiClient;

    public AccountClientService(IFUNewsApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAccountsAsync(odataQuery, cancellationToken);
    }

    public Task<AccountApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAccountByIdAsync(id, cancellationToken);
    }

    public Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.CreateAccountAsync(request, cancellationToken);
    }

    public Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.UpdateAccountAsync(id, request, cancellationToken);
    }

    public Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAccountAsync(id, cancellationToken);
    }

    public Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return _apiClient.GetProfileAsync(cancellationToken);
    }

    public Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.UpdateProfileAsync(request, cancellationToken);
    }

    public Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.ChangePasswordAsync(request, cancellationToken);
    }
}
