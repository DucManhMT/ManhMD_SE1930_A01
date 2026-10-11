using System.Net.Http.Json;
using System.Text.Json;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public class AnalyticsApiClient : IAnalyticsApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AnalyticsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<AnalyticsDashboardApiModel> GetDashboardAsync(DateTime? startDate = null, DateTime? endDate = null, short? categoryId = null, bool? status = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("yyyy-MM-dd"))}");
        if (endDate.HasValue) queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("yyyy-MM-dd"))}");
        if (categoryId.HasValue) queryParams.Add($"categoryId={categoryId.Value}");
        if (status.HasValue) queryParams.Add($"status={status.Value.ToString().ToLowerInvariant()}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var uri = $"api/analytics/dashboard{queryString}";

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<AnalyticsDashboardApiModel>(JsonOptions, cancellationToken);
        return result ?? new AnalyticsDashboardApiModel();
    }

    public async Task<List<TrendingArticleApiModel>> GetTrendingArticlesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var uri = $"api/analytics/trending?limit={limit}";
        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<List<TrendingArticleApiModel>>(JsonOptions, cancellationToken);
        return result ?? new List<TrendingArticleApiModel>();
    }

    public async Task<List<RecommendedArticleApiModel>> GetRecommendedArticlesAsync(string articleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);

        var uri = $"api/recommend/{Uri.EscapeDataString(articleId)}";
        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<List<RecommendedArticleApiModel>>(JsonOptions, cancellationToken);
        return result ?? new List<RecommendedArticleApiModel>();
    }

    public async Task<byte[]> ExportExcelAsync(DateTime? startDate = null, DateTime? endDate = null, short? categoryId = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("yyyy-MM-dd"))}");
        if (endDate.HasValue) queryParams.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("yyyy-MM-dd"))}");
        if (categoryId.HasValue) queryParams.Add($"categoryId={categoryId.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var uri = $"api/analytics/export{queryString}";

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
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
