using System.Net;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FUNews.Tests;

public class RefreshTokenFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RefreshTokenFlowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Criterion_01_Login_ShouldReturn_AccessToken_And_RefreshToken()
    {
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));
        Assert.False(string.IsNullOrWhiteSpace(loginResult.RefreshToken));
        Assert.NotNull(loginResult.User);
    }

    [Fact]
    public async Task Criterion_02_RefreshToken_ShouldRotate_And_IssueNewTokenPair()
    {
        // 1. Đăng nhập để nhận token ban đầu
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };

        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);
        var initialRefreshToken = loginResult.RefreshToken;

        // 2. Gửi request refresh token
        var refreshRequest = new RefreshTokenRequestDto
        {
            RefreshToken = initialRefreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("api/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(refreshResult);
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.Token));
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.RefreshToken));
        Assert.NotEqual(initialRefreshToken, refreshResult.RefreshToken);

        // 3. Sử dụng lại token cũ phải bị từ chối (Token Rotation)
        var reuseResponse = await _client.PostAsJsonAsync("api/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Criterion_03_Logout_ShouldRevoke_RefreshToken()
    {
        // 1. Đăng nhập
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };

        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);

        // 2. Đăng xuất
        var logoutRequest = new RefreshTokenRequestDto
        {
            RefreshToken = loginResult.RefreshToken
        };

        var logoutResponse = await _client.PostAsJsonAsync("api/auth/logout", logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 3. Dùng refresh token sau khi logout phải bị từ chối
        var refreshResponse = await _client.PostAsJsonAsync("api/auth/refresh", logoutRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
