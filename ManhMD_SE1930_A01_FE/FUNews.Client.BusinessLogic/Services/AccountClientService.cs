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
}
