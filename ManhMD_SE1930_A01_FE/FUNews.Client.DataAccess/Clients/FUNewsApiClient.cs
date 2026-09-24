using System.Net.Http.Json;
using System.Text.Json;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public class FUNewsApiClient : IFUNewsApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FUNewsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/category" : $"api/category{FormatQuery(odataQuery)}";
        return GetEnvelopeAsync<CategoryApiModel>(uri, cancellationToken);
    }

    public Task<CategoryApiModel?> GetCategoryByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return GetSingleAsync<CategoryApiModel>($"api/category/{id}", cancellationToken);
    }

    public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/tag" : $"api/tag{FormatQuery(odataQuery)}";
        return GetEnvelopeAsync<TagApiModel>(uri, cancellationToken);
    }

    public Task<TagApiModel?> GetTagByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return GetSingleAsync<TagApiModel>($"api/tag/{id}", cancellationToken);
    }

    public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/news" : $"api/news{FormatQuery(odataQuery)}";
        return GetEnvelopeAsync<NewsArticleApiModel>(uri, cancellationToken);
    }

    public Task<NewsArticleApiModel?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return GetSingleAsync<NewsArticleApiModel>($"api/news/{id}", cancellationToken);
    }

    public Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/account" : $"api/account{FormatQuery(odataQuery)}";
        return GetEnvelopeAsync<AccountApiModel>(uri, cancellationToken);
    }

    public Task<AccountApiModel?> GetAccountByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return GetSingleAsync<AccountApiModel>($"api/account/{id}", cancellationToken);
    }

    public async Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync("api/account", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<AccountApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PutAsJsonAsync($"api/account/{id}", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<AccountApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/account/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    public async Task<LoginResponseApiModel> LoginAsync(LoginRequestApiModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    private static string FormatQuery(string query)
    {
        var trimmed = query.Trim();
        return trimmed.StartsWith('?') ? trimmed : $"?{trimmed}";
    }

    private async Task<ODataEnvelope<T>> GetEnvelopeAsync<T>(string relativeUri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(relativeUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<ODataEnvelope<T>>(JsonOptions, cancellationToken);
        return result ?? new ODataEnvelope<T>();
    }

    private async Task<T?> GetSingleAsync<T>(string relativeUri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(relativeUri, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(JsonOptions, cancellationToken);
        }
        catch
        {
            // Failed to parse ProblemDetails JSON
        }

        var message = problem?.Detail ?? problem?.Title ?? $"API error: {response.StatusCode}";
        throw new FUNewsApiException(response.StatusCode, message, problem);
    }
}
