using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Extensions;
using FUNews.Client.DataAccess.Handlers;
using FUNews.Client.DataAccess.Models;
using FUNews.Client.DataAccess.Policies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Client.Tests;

public class DistributedClientInfrastructureTests
{
    private class TestTokenProvider : ITokenProvider
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public bool WasCleared { get; private set; }

        public TestTokenProvider(string? token = null, string? refreshToken = null)
        {
            Token = token;
            RefreshToken = refreshToken;
        }

        public string? GetToken() => Token;
        public string? GetRefreshToken() => RefreshToken;

        public void SaveTokens(string accessToken, string refreshToken, DateTime? expiresAt = null)
        {
            Token = accessToken;
            RefreshToken = refreshToken;
        }

        public void ClearTokens()
        {
            Token = null;
            RefreshToken = null;
            WasCleared = true;
        }
    }

    private class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public MockHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public List<HttpRequestMessage> Requests { get; } = new();

        public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_responses.Count > 0)
            {
                return Task.FromResult(_responses.Dequeue());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task AuthTokenDelegatingHandler_ShouldAttachBearerToken_WhenTokenExists()
    {
        // Arrange
        var tokenProvider = new TestTokenProvider("sample.access.token");
        var sequenceHandler = new SequenceHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new AuthTokenDelegatingHandler(tokenProvider)
        {
            InnerHandler = sequenceHandler
        };

        var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://localhost:7001/api/news");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(sequenceHandler.Requests);
        Assert.NotNull(sequenceHandler.Requests[0].Headers.Authorization);
        Assert.Equal("Bearer", sequenceHandler.Requests[0].Headers.Authorization?.Scheme);
        Assert.Equal("sample.access.token", sequenceHandler.Requests[0].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task AuthTokenDelegatingHandler_ShouldAutomaticallyRefreshToken_AndRetryOriginalRequest_WhenReceiving401()
    {
        // Arrange
        var tokenProvider = new TestTokenProvider("expired.access.token", "valid.refresh.token");

        // Mock response khi gọi refresh endpoint: 200 OK với token mới
        var refreshResponsePayload = new LoginResponseApiModel
        {
            AccessToken = "new.fresh.access.token",
            RefreshToken = "new.fresh.refresh.token",
            Token = "new.fresh.access.token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
        var refreshMockHandler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(refreshResponsePayload)
            }
        );
        var refreshClient = new HttpClient(refreshMockHandler)
        {
            BaseAddress = new Uri("https://localhost:7001/")
        };
        var clientFactory = new MockHttpClientFactory(refreshClient);

        // Mock inner handler của request chính: lần 1 trả 401, lần 2 trả 200 OK
        var mainMockHandler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK)
        );

        var handler = new AuthTokenDelegatingHandler(tokenProvider, clientFactory)
        {
            InnerHandler = mainMockHandler
        };
        var mainClient = new HttpClient(handler);

        // Act
        var result = await mainClient.GetAsync("https://localhost:7001/api/news");

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        // TokenProvider phải được cập nhật token mới
        Assert.Equal("new.fresh.access.token", tokenProvider.Token);
        Assert.Equal("new.fresh.refresh.token", tokenProvider.RefreshToken);

        // Phải có 2 request tới mainMockHandler (1 lần 401, 1 lần retry với token mới)
        Assert.Equal(2, mainMockHandler.Requests.Count);
        Assert.Equal("Bearer", mainMockHandler.Requests[1].Headers.Authorization?.Scheme);
        Assert.Equal("new.fresh.access.token", mainMockHandler.Requests[1].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task AuthTokenDelegatingHandler_ShouldClearTokens_WhenRefreshFailsWithUnauthorized()
    {
        // Arrange
        var tokenProvider = new TestTokenProvider("expired.access.token", "invalid.refresh.token");

        // Refresh client trả về 401 (refresh token bị hết hạn/thu hồi)
        var refreshMockHandler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
        );
        var refreshClient = new HttpClient(refreshMockHandler)
        {
            BaseAddress = new Uri("https://localhost:7001/")
        };
        var clientFactory = new MockHttpClientFactory(refreshClient);

        var mainMockHandler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
        );

        var handler = new AuthTokenDelegatingHandler(tokenProvider, clientFactory)
        {
            InnerHandler = mainMockHandler
        };
        var mainClient = new HttpClient(handler);

        // Act
        var result = await mainClient.GetAsync("https://localhost:7001/api/news");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.True(tokenProvider.WasCleared);
        Assert.Null(tokenProvider.Token);
        Assert.Null(tokenProvider.RefreshToken);
    }

    [Fact]
    public void PollyPolicy_ShouldRetryGetRequests_AndNotRetryPostRequests()
    {
        // Arrange
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7001/api/news");
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7001/api/news");

        // Act
        var getPolicy = PollyPolicies.GetSelectableRetryPolicy(getRequest);
        var postPolicy = PollyPolicies.GetSelectableRetryPolicy(postRequest);

        // Assert
        Assert.NotNull(getPolicy);
        Assert.NotNull(postPolicy);
        // GET policy là policy retry có cấu hình, POST policy là NoOpPolicy
        Assert.NotSame(getPolicy, postPolicy);
    }

    [Fact]
    public void DependencyInjection_ShouldRegisterThreeClients_AndBackwardCompatibleAlias()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFUNewsClientDataAccess(
            "https://localhost:7001",
            "https://localhost:7002",
            "https://localhost:7003"
        );
        var provider = services.BuildServiceProvider();

        // Act
        var coreClient = provider.GetService<ICoreApiClient>();
        var legacyClient = provider.GetService<IFUNewsApiClient>();
        var analyticsClient = provider.GetService<IAnalyticsApiClient>();
        var aiClient = provider.GetService<IAiApiClient>();

        // Assert
        Assert.NotNull(coreClient);
        Assert.NotNull(legacyClient);
        Assert.NotNull(analyticsClient);
        Assert.NotNull(aiClient);
        Assert.IsType<CoreApiClient>(coreClient);
        Assert.IsAssignableFrom<IFUNewsApiClient>(coreClient);
        Assert.IsType<AnalyticsApiClient>(analyticsClient);
        Assert.IsType<AiApiClient>(aiClient);
    }
}
