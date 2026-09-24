using System.ComponentModel.DataAnnotations;
using System.Net;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;
using Xunit;

namespace FUNews.Client.Tests;

public class ClientAuthenticationTests
{
    private class TestTokenProvider : ITokenProvider
    {
        private readonly string? _token;

        public TestTokenProvider(string? token)
        {
            _token = token;
        }

        public string? GetToken() => _token;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task AuthHeaderHandler_ShouldAttachBearerToken_WhenTokenProviderReturnsToken()
    {
        var tokenProvider = new TestTokenProvider("test.jwt.token");
        var mockInner = new MockHttpMessageHandler();
        var handler = new AuthHeaderHandler(tokenProvider)
        {
            InnerHandler = mockInner
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7001/api/news");

        await client.SendAsync(request);

        Assert.NotNull(mockInner.LastRequest);
        Assert.NotNull(mockInner.LastRequest.Headers.Authorization);
        Assert.Equal("Bearer", mockInner.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal("test.jwt.token", mockInner.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task AuthHeaderHandler_ShouldNotAttachBearerToken_WhenTokenProviderReturnsNull()
    {
        var tokenProvider = new TestTokenProvider(null);
        var mockInner = new MockHttpMessageHandler();
        var handler = new AuthHeaderHandler(tokenProvider)
        {
            InnerHandler = mockInner
        };

        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7001/api/news");

        await client.SendAsync(request);

        Assert.NotNull(mockInner.LastRequest);
        Assert.Null(mockInner.LastRequest.Headers.Authorization);
    }

    [Fact]
    public void LoginRequestApiModel_Validation_ShouldEnforce_EmailAndPasswordRequired()
    {
        var emptyModel = new LoginRequestApiModel();
        var context = new ValidationContext(emptyModel);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(emptyModel, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void LoginRequestApiModel_Validation_ShouldDetect_InvalidEmailFormat()
    {
        var model = new LoginRequestApiModel
        {
            Email = "not-an-email",
            Password = "valid_password"
        };
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void UserInfoApiModel_Admin_ShouldHave_NullAccountId()
    {
        var adminInfo = new UserInfoApiModel
        {
            AccountId = null,
            AccountName = "Quản trị viên",
            AccountEmail = "admin@FUNewsManagementSystem.org",
            AccountRole = null,
            RoleName = "Admin"
        };

        Assert.Null(adminInfo.AccountId);
        Assert.Equal("Admin", adminInfo.RoleName);
    }
}
