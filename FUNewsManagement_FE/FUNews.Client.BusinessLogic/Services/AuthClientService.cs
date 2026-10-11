using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class AuthClientService : IAuthClientService
{
    private readonly IFUNewsApiClient _apiClient;

    public AuthClientService(IFUNewsApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<LoginResponseApiModel> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequestApiModel
        {
            Email = email?.Trim() ?? string.Empty,
            Password = password ?? string.Empty
        };

        return _apiClient.LoginAsync(request, cancellationToken);
    }
}
