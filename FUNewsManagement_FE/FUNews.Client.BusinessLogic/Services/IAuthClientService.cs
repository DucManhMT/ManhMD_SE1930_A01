using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IAuthClientService
{
    Task<LoginResponseApiModel> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
