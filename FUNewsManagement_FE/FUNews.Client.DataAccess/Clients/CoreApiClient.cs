using System.Net.Http.Json;
using System.Text.Json;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;

using FUNews.Client.DataAccess.Services;

namespace FUNews.Client.DataAccess.Clients;

public class CoreApiClient : FUNewsApiClient, ICoreApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CoreApiClient(HttpClient httpClient, IOfflineStoreManager? offlineStore = null) : base(httpClient, offlineStore)
    {
    }

    public async Task<LoginResponseApiModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var payload = new RefreshTokenRequestApiModel { RefreshToken = refreshToken };
        using var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", payload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseApiModel>(JsonOptions, cancellationToken);
        return result ?? throw new FUNewsApiException(response.StatusCode, "Không nhận được phản hồi làm mới token từ Core API.");
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var payload = new RefreshTokenRequestApiModel { RefreshToken = refreshToken };
        using var response = await _httpClient.PostAsJsonAsync("api/auth/logout", payload, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }
    }

    public async Task<AuditLogPagedResponseApiModel> GetAuditLogsAsync(
        int? userId = null,
        string? entityType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (userId.HasValue)
        {
            queryParams.Add($"userId={userId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            queryParams.Add($"entityType={Uri.EscapeDataString(entityType.Trim())}");
        }

        var queryString = string.Join("&", queryParams);
        var uri = $"api/auditlog?{queryString}";

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<AuditLogPagedResponseApiModel>(JsonOptions, cancellationToken);
        return result ?? new AuditLogPagedResponseApiModel();
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
