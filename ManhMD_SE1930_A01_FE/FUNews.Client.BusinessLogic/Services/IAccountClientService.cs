using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IAccountClientService
{
    Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<AccountApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
}
