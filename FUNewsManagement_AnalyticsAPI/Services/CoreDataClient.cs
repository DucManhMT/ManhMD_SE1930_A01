using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FUNewsManagement_AnalyticsAPI.Models;
using Microsoft.AspNetCore.Http;

namespace FUNewsManagement_AnalyticsAPI.Services;

public class CoreDataClient : ICoreDataClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CoreDataClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private void AttachAuthorizationHeader(HttpRequestMessage request)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<CoreNewsArticleDto>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/news");
        AttachAuthorizationHeader(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new List<CoreNewsArticleDto>();
        }

        var result = await response.Content.ReadFromJsonAsync<CoreODataResponse<CoreNewsArticleDto>>(JsonOptions, cancellationToken);
        return result?.Value ?? new List<CoreNewsArticleDto>();
    }

    public async Task<List<CoreNewsArticleDto>> GetActiveNewsArticlesAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/news?activeOnly=true");
        AttachAuthorizationHeader(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new List<CoreNewsArticleDto>();
        }

        var result = await response.Content.ReadFromJsonAsync<CoreODataResponse<CoreNewsArticleDto>>(JsonOptions, cancellationToken);
        return result?.Value ?? new List<CoreNewsArticleDto>();
    }

    public async Task<CoreNewsArticleDto?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/news/{Uri.EscapeDataString(id)}");
        AttachAuthorizationHeader(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CoreNewsArticleDto>(JsonOptions, cancellationToken);
    }

    public async Task<List<CoreCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/category");
        AttachAuthorizationHeader(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new List<CoreCategoryDto>();
        }

        var result = await response.Content.ReadFromJsonAsync<CoreODataResponse<CoreCategoryDto>>(JsonOptions, cancellationToken);
        return result?.Value ?? new List<CoreCategoryDto>();
    }

    public async Task<Dictionary<string, int>> GetViewStatsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/news/views/stats");
        AttachAuthorizationHeader(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new Dictionary<string, int>();
        }

        return await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(JsonOptions, cancellationToken)
               ?? new Dictionary<string, int>();
    }
}
