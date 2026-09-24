using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public interface IFUNewsApiClient
{
    Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<CategoryApiModel?> GetCategoryByIdAsync(short id, CancellationToken cancellationToken = default);

    Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<TagApiModel?> GetTagByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<NewsArticleApiModel?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<AccountApiModel?> GetAccountByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default);

    Task<LoginResponseApiModel> LoginAsync(LoginRequestApiModel request, CancellationToken cancellationToken = default);
}
