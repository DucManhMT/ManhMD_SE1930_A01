using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using FUNews.Client.DataAccess.Services;

namespace FUNews.Client.DataAccess.Clients;

public class FUNewsApiClient : IFUNewsApiClient
{
    protected readonly HttpClient _httpClient;
    protected readonly IOfflineStoreManager? _offlineStore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FUNewsApiClient(HttpClient httpClient, IOfflineStoreManager? offlineStore = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _offlineStore = offlineStore;
    }

    private static bool IsNetworkOrServerFailure(Exception ex)
    {
        if (ex is HttpRequestException || ex is TimeoutException) return true;
        if (ex is FUNewsApiException apiEx)
        {
            var code = (int)apiEx.StatusCode;
            return code == 503 || code == 502 || code == 504 || code == 500;
        }
        return false;
    }

    private void EnsureNotOffline()
    {
        if (_offlineStore?.IsOffline == true)
        {
            throw new FUNewsApiException(System.Net.HttpStatusCode.ServiceUnavailable,
                "Hệ thống đang hoạt động ở chế độ ngoại tuyến (Offline Mode). Các thao tác thêm, sửa, xóa tạm thời bị khóa.");
        }
    }

    public async Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/category" : $"api/category{FormatQuery(odataQuery)}";
            var result = await GetEnvelopeAsync<CategoryApiModel>(uri, cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return new ODataEnvelope<CategoryApiModel>
                {
                    Count = snapshot.Categories.Count,
                    Value = snapshot.Categories
                };
            }
            throw;
        }
    }

    public async Task<CategoryApiModel?> GetCategoryByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetSingleAsync<CategoryApiModel>($"api/category/{id}", cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return snapshot.Categories.FirstOrDefault(c => c.CategoryId == id);
            }
            throw;
        }
    }

    public async Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync("api/category", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<CategoryApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PutAsJsonAsync($"api/category/{id}", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<CategoryApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        using var response = await _httpClient.DeleteAsync($"api/category/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    public async Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/tag" : $"api/tag{FormatQuery(odataQuery)}";
            var result = await GetEnvelopeAsync<TagApiModel>(uri, cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return new ODataEnvelope<TagApiModel>
                {
                    Count = snapshot.Tags.Count,
                    Value = snapshot.Tags
                };
            }
            throw;
        }
    }

    public async Task<TagApiModel?> GetTagByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetSingleAsync<TagApiModel>($"api/tag/{id}", cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return snapshot.Tags.FirstOrDefault(t => t.TagId == id);
            }
            throw;
        }
    }

    public async Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync("api/tag", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<TagApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PutAsJsonAsync($"api/tag/{id}", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<TagApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task DeleteTagAsync(int id, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        using var response = await _httpClient.DeleteAsync($"api/tag/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    public async Task<List<NewsArticleApiModel>> GetNewsArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/tag/{tagId}/news", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<List<NewsArticleApiModel>>(JsonOptions, cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result ?? new List<NewsArticleApiModel>();
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return snapshot.Articles.Where(a => a.Tags != null && a.Tags.Any(t => t.TagId == tagId)).ToList();
            }
            throw;
        }
    }

    public async Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/news" : $"api/news{FormatQuery(odataQuery)}";
            var result = await GetEnvelopeAsync<NewsArticleApiModel>(uri, cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return new ODataEnvelope<NewsArticleApiModel>
                {
                    Count = snapshot.Articles.Count,
                    Value = snapshot.Articles
                };
            }
            throw;
        }
    }

    public async Task<NewsArticleApiModel?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await GetSingleAsync<NewsArticleApiModel>($"api/news/{id}", cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return snapshot.Articles.FirstOrDefault(a => string.Equals(a.NewsArticleId, id, StringComparison.OrdinalIgnoreCase));
            }
            throw;
        }
    }

    public async Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync("api/news", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var created = await response.Content.ReadFromJsonAsync<NewsArticleApiModel>(JsonOptions, cancellationToken);
        return created ?? throw new InvalidOperationException("Máy chủ không trả về dữ liệu bài viết vừa tạo.");
    }

    public async Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PutAsJsonAsync($"api/news/{id}", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var updated = await response.Content.ReadFromJsonAsync<NewsArticleApiModel>(JsonOptions, cancellationToken);
        return updated ?? throw new InvalidOperationException("Máy chủ không trả về dữ liệu bài viết vừa cập nhật.");
    }

    public async Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        using var response = await _httpClient.DeleteAsync($"api/news/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    public async Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        using var response = await _httpClient.PostAsync($"api/news/{Uri.EscapeDataString(id)}/duplicate", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var duplicated = await response.Content.ReadFromJsonAsync<NewsArticleApiModel>(JsonOptions, cancellationToken);
        return duplicated ?? throw new InvalidOperationException("Máy chủ không trả về dữ liệu bài viết vừa nhân bản.");
    }

    public async Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = string.IsNullOrWhiteSpace(odataQuery) ? "api/news/mine" : $"api/news/mine{FormatQuery(odataQuery)}";
            var result = await GetEnvelopeAsync<NewsArticleApiModel>(uri, cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result;
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                return new ODataEnvelope<NewsArticleApiModel>
                {
                    Count = snapshot.Articles.Count,
                    Value = snapshot.Articles
                };
            }
            throw;
        }
    }

    public async Task<List<NewsArticleApiModel>> GetRelatedNewsArticlesAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/news/{Uri.EscapeDataString(id)}/related", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<List<NewsArticleApiModel>>(JsonOptions, cancellationToken);
            _offlineStore?.MarkApiSuccess();
            return result ?? new List<NewsArticleApiModel>();
        }
        catch (Exception ex) when (IsNetworkOrServerFailure(ex))
        {
            _offlineStore?.MarkApiFailure();
            var snapshot = _offlineStore?.GetCachedSnapshot() ?? await (_offlineStore?.GetSnapshotAsync(cancellationToken) ?? Task.FromResult<OfflineDataSnapshot?>(null));
            if (snapshot != null)
            {
                var cur = snapshot.Articles.FirstOrDefault(a => string.Equals(a.NewsArticleId, id, StringComparison.OrdinalIgnoreCase));
                if (cur != null)
                {
                    return snapshot.Articles.Where(a => a.NewsArticleId != id && (a.CategoryId == cur.CategoryId)).Take(3).ToList();
                }
            }
            throw;
        }
    }

    public async Task<string> UploadNewsImageAsync(string articleId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);
        ArgumentNullException.ThrowIfNull(fileStream);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(streamContent, "file", fileName);

        using var response = await _httpClient.PostAsync($"api/news/{Uri.EscapeDataString(articleId)}/image", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<UploadImageResponseApiModel>(JsonOptions, cancellationToken);
        return result?.ImageUrl ?? string.Empty;
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
        EnsureNotOffline();
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
        EnsureNotOffline();
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
        EnsureNotOffline();
        using var response = await _httpClient.DeleteAsync($"api/account/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    public Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return GetSingleAsync<AccountApiModel>("api/account/profile", cancellationToken)!;
    }

    public async Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PutAsJsonAsync("api/account/profile", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<AccountApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi từ máy chủ.");
    }

    public async Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default)
    {
        EnsureNotOffline();
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PutAsJsonAsync("api/account/password", request, JsonOptions, cancellationToken);
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

    public async Task<NewsReportApiModel> GetReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null, CancellationToken cancellationToken = default)
    {
        var startStr = Uri.EscapeDataString(startDate.ToString("yyyy-MM-dd"));
        var endStr = Uri.EscapeDataString(endDate.ToString("yyyy-MM-dd"));
        var groupParam = string.IsNullOrWhiteSpace(groupBy) ? "" : $"&groupBy={Uri.EscapeDataString(groupBy)}";

        var uri = $"api/report?startDate={startStr}&endDate={endStr}{groupParam}";
        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var report = await response.Content.ReadFromJsonAsync<NewsReportApiModel>(JsonOptions, cancellationToken);
        return report ?? new NewsReportApiModel();
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
