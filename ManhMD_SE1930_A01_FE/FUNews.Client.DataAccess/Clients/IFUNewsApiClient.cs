using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public interface IFUNewsApiClient
{
    Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<CategoryApiModel?> GetCategoryByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default);
    Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default);

    Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<TagApiModel?> GetTagByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default);
    Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(int id, CancellationToken cancellationToken = default);
    Task<List<NewsArticleApiModel>> GetNewsArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default);

    Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<NewsArticleApiModel?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<AccountApiModel?> GetAccountByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default);
    Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default);
    Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default);

    Task<LoginResponseApiModel> LoginAsync(LoginRequestApiModel request, CancellationToken cancellationToken = default);
}
