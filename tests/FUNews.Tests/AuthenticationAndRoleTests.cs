using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Options;
using FUNews.BusinessLogic.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FUNews.Tests;

public class AuthenticationAndRoleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationAndRoleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Criterion_01_Admin_ShouldLoginSuccessfully_WithConfigurationCredentials()
    {
        // 1. Admin login with configuration email & password from appsettings.json
        var request = new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        Assert.NotNull(result.User);
        Assert.Equal("Admin", result.User.RoleName);
        Assert.Equal("admin@FUNewsManagementSystem.org", result.User.AccountEmail);
        Assert.Equal("Quản trị viên", result.User.AccountName);

        // Tiêu chí 7: Admin KHÔNG có AccountID trong DB và KHÔNG được gán AccountID giả
        Assert.Null(result.User.AccountId);
        Assert.Null(result.User.AccountRole);
    }

    [Fact]
    public async Task Criterion_01_Staff_ShouldLoginSuccessfully_WithDatabaseCredentials()
    {
        // 2. Staff login with database account (Isabella David, AccountID=3, Role=1)
        var request = new LoginRequestDto
        {
            Email = "IsabellaDavid@FUNewsManagement.org",
            Password = "@1"
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.NotNull(result.User);
        Assert.Equal("Staff", result.User.RoleName);
        Assert.Equal((short)3, result.User.AccountId);
        Assert.Equal(1, result.User.AccountRole);
        Assert.Equal("Isabella David", result.User.AccountName);
    }

    [Fact]
    public async Task Criterion_01_Lecturer_ShouldLoginSuccessfully_WithDatabaseCredentials()
    {
        // 3. Lecturer login with database account (Emma William, AccountID=1, Role=2)
        var request = new LoginRequestDto
        {
            Email = "EmmaWilliam@FUNewsManagement.org",
            Password = "@1"
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.NotNull(result.User);
        Assert.Equal("Lecturer", result.User.RoleName);
        Assert.Equal((short)1, result.User.AccountId);
        Assert.Equal(2, result.User.AccountRole);
        Assert.Equal("Emma William", result.User.AccountName);
    }

    [Theory]
    [InlineData("admin@FUNewsManagementSystem.org", "WrongAdminPass!")]
    [InlineData("IsabellaDavid@FUNewsManagement.org", "WrongStaffPass!")]
    [InlineData("EmmaWilliam@FUNewsManagement.org", "WrongLecturerPass!")]
    [InlineData("nonexistent@FUNewsManagement.org", "@1")]
    public async Task Criterion_02_InvalidCredentials_ShouldReturn_401Unauthorized(string email, string password)
    {
        var request = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Phản hồi lỗi chuẩn RFC 7807 ProblemDetails
        var rawString = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email hoặc mật khẩu không chính xác", rawString);
    }

    [Fact]
    public async Task Criterion_03_GeneratedJwtToken_ShouldContain_ExpectedClaims()
    {
        // Lấy token cho Staff
        var request = new LoginRequestDto
        {
            Email = "IsabellaDavid@FUNewsManagement.org",
            Password = "@1"
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        Assert.Equal("FUNewsApi", jwtToken.Issuer);
        Assert.Equal("FUNewsWeb", jwtToken.Audiences.First());

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("Staff", roleClaim.Value);

        var accountIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "accountId");
        Assert.NotNull(accountIdClaim);
        Assert.Equal("3", accountIdClaim.Value);
    }

    [Fact]
    public async Task Criterion_07_AdminToken_ShouldNotContain_FakeAccountIdClaim()
    {
        // Lấy token cho Admin
        var request = new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };

        var response = await _client.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        // Kiểm tra không có bất kỳ claim accountId nào
        var accountIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "accountId" || c.Type == "AccountId");
        Assert.Null(accountIdClaim);

        // NameIdentifier của Admin phải là Email, không phải số ID giả
        var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
        Assert.NotNull(nameIdClaim);
        Assert.Equal("admin@FUNewsManagementSystem.org", nameIdClaim.Value);
        Assert.False(int.TryParse(nameIdClaim.Value, out _), "Admin NameIdentifier không được là số ID giả.");
    }

    [Fact]
    public async Task Criterion_06_Lecturer_ShouldBeForbidden_FromCallingWriteApis()
    {
        // 1. Đăng nhập với tài khoản Lecturer
        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new LoginRequestDto
        {
            Email = "EmmaWilliam@FUNewsManagement.org",
            Password = "@1"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);

        var lecturerClient = _factory.CreateClient();
        lecturerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        // 2. Lecturer gọi POST /api/category -> Bị 403 Forbidden
        var categoryPost = await lecturerClient.PostAsJsonAsync("api/category", new { });
        Assert.Equal(HttpStatusCode.Forbidden, categoryPost.StatusCode);

        // 3. Lecturer gọi PUT /api/category/1 -> Bị 403 Forbidden
        var categoryPut = await lecturerClient.PutAsJsonAsync("api/category/1", new { });
        Assert.Equal(HttpStatusCode.Forbidden, categoryPut.StatusCode);

        // 4. Lecturer gọi DELETE /api/category/1 -> Bị 403 Forbidden
        var categoryDelete = await lecturerClient.DeleteAsync("api/category/1");
        Assert.Equal(HttpStatusCode.Forbidden, categoryDelete.StatusCode);

        // 5. Lecturer gọi POST /api/tag -> Bị 403 Forbidden
        var tagPost = await lecturerClient.PostAsJsonAsync("api/tag", new { });
        Assert.Equal(HttpStatusCode.Forbidden, tagPost.StatusCode);

        // 6. Lecturer gọi POST /api/news -> Bị 403 Forbidden
        var newsPost = await lecturerClient.PostAsJsonAsync("api/news", new { });
        Assert.Equal(HttpStatusCode.Forbidden, newsPost.StatusCode);

        // 7. Lecturer gọi POST /api/news/N1/duplicate -> Bị 403 Forbidden
        var duplicatePost = await lecturerClient.PostAsJsonAsync("api/news/N1/duplicate", new { });
        Assert.Equal(HttpStatusCode.Forbidden, duplicatePost.StatusCode);

        // 8. Lecturer gọi GET /api/account -> Bị 403 Forbidden (chỉ Admin mới được xem tài khoản)
        var accountGet = await lecturerClient.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.Forbidden, accountGet.StatusCode);
    }

    [Fact]
    public async Task Criterion_06_Staff_ShouldBeAuthorized_ForWriteApis()
    {
        // 1. Đăng nhập với tài khoản Staff
        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new LoginRequestDto
        {
            Email = "IsabellaDavid@FUNewsManagement.org",
            Password = "@1"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        // 2. Staff gọi POST /api/category -> Được ủy quyền (vượt qua auth check, không bị 401 hoặc 403)
        var categoryPost = await staffClient.PostAsJsonAsync("api/category", new { });
        Assert.NotEqual(HttpStatusCode.Unauthorized, categoryPost.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, categoryPost.StatusCode);
        Assert.True(categoryPost.StatusCode == HttpStatusCode.BadRequest || categoryPost.StatusCode == HttpStatusCode.NotImplemented);

        // 3. Staff gọi POST /api/tag -> Được ủy quyền (vượt qua auth check, không bị 401 hoặc 403)
        var tagPost = await staffClient.PostAsJsonAsync("api/tag", new { });
        Assert.NotEqual(HttpStatusCode.Unauthorized, tagPost.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, tagPost.StatusCode);
        Assert.True(tagPost.StatusCode == HttpStatusCode.BadRequest || tagPost.StatusCode == HttpStatusCode.NotImplemented);

        // 4. Staff gọi POST /api/news -> Trả 501 (vượt qua auth check)
        var newsPost = await staffClient.PostAsJsonAsync("api/news", new { });
        Assert.Equal(HttpStatusCode.NotImplemented, newsPost.StatusCode);
    }

    [Fact]
    public async Task Criterion_06_Admin_ShouldBeAuthorized_ForAccountApis()
    {
        // 1. Đăng nhập với tài khoản Admin
        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        // 2. Admin gọi GET /api/account -> 200 OK
        var accountGet = await adminClient.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.OK, accountGet.StatusCode);
    }

    [Fact]
    public void JwtTokenService_KeyShorterThan32Bytes_ThrowsInvalidOperationException()
    {
        var shortKeyOptions = new JwtOptions
        {
            Issuer = "Test",
            Audience = "Test",
            SigningKey = "short_key_under_32_bytes"
        };

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(shortKeyOptions));
    }

    [Fact]
    public void JwtTokenService_ValidKey_InstantiatesSuccessfully()
    {
        var validKeyOptions = new JwtOptions
        {
            Issuer = "Test",
            Audience = "Test",
            SigningKey = "ThisIsAValid32ByteOrMoreSigningKeyForUnitTesting_123456"
        };

        var service = new JwtTokenService(validKeyOptions);
        Assert.NotNull(service);
    }
}
