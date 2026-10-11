using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;
using Microsoft.Extensions.Logging;

namespace FUNews.Client.DataAccess.Handlers;

public class AuthTokenDelegatingHandler : DelegatingHandler
{
    private readonly ITokenProvider? _tokenProvider;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<AuthTokenDelegatingHandler>? _logger;
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public AuthTokenDelegatingHandler(
        ITokenProvider? tokenProvider = null,
        IHttpClientFactory? httpClientFactory = null,
        ILogger<AuthTokenDelegatingHandler>? logger = null)
    {
        _tokenProvider = tokenProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isAuthRequest = request.RequestUri != null &&
            (request.RequestUri.AbsolutePath.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
             request.RequestUri.AbsolutePath.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase));

        string? initialToken = null;

        if (_tokenProvider != null && !isAuthRequest)
        {
            initialToken = _tokenProvider.GetToken();
            if (!string.IsNullOrWhiteSpace(initialToken) && request.Headers.Authorization == null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", initialToken);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && _tokenProvider != null && !isAuthRequest && _httpClientFactory != null)
        {
            var refreshToken = _tokenProvider.GetRefreshToken();
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await RefreshLock.WaitAsync(cancellationToken);
                try
                {
                    var currentToken = _tokenProvider.GetToken();
                    if (!string.IsNullOrWhiteSpace(currentToken) && currentToken != initialToken)
                    {
                        // Token đã được làm mới thành công bởi luồng khác, gửi lại request với token mới
                        var retryReq = await CloneHttpRequestMessageAsync(request);
                        retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                        return await base.SendAsync(retryReq, cancellationToken);
                    }

                    _logger?.LogInformation("AuthTokenDelegatingHandler: Nhận mã 401, tiến hành tự động làm mới Access Token...");

                    var refreshClient = _httpClientFactory.CreateClient("AuthRefreshClient");
                    var payload = new RefreshTokenRequestApiModel { RefreshToken = refreshToken };
                    var refreshResponse = await refreshClient.PostAsJsonAsync("api/auth/refresh", payload, cancellationToken);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponseApiModel>(cancellationToken: cancellationToken);
                        if (refreshResult != null)
                        {
                            var newAccessToken = !string.IsNullOrWhiteSpace(refreshResult.AccessToken)
                                ? refreshResult.AccessToken
                                : refreshResult.Token;
                            var newRefreshToken = !string.IsNullOrWhiteSpace(refreshResult.RefreshToken)
                                ? refreshResult.RefreshToken
                                : refreshToken;

                            if (!string.IsNullOrWhiteSpace(newAccessToken))
                            {
                                _logger?.LogInformation("AuthTokenDelegatingHandler: Làm mới Token thành công, cập nhật phiên làm việc.");
                                _tokenProvider.SaveTokens(newAccessToken, newRefreshToken, refreshResult.ExpiresAt);

                                var retryReq = await CloneHttpRequestMessageAsync(request);
                                retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                                return await base.SendAsync(retryReq, cancellationToken);
                            }
                        }
                    }

                    _logger?.LogWarning("AuthTokenDelegatingHandler: Làm mới token thất bại. Xóa phiên đăng nhập.");
                    _tokenProvider.ClearTokens();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "AuthTokenDelegatingHandler: Lỗi ngoại lệ trong quá trình refresh token.");
                    _tokenProvider.ClearTokens();
                }
                finally
                {
                    RefreshLock.Release();
                }
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var memoryStream = new MemoryStream();
            await request.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            clone.Content = new StreamContent(memoryStream);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
