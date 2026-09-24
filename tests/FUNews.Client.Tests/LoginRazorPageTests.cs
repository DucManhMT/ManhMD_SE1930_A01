using System.Security.Claims;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Client.Tests;

public class LoginRazorPageTests
{
    private class FakeAuthClientService : IAuthClientService
    {
        public bool ShouldFail { get; set; }
        public UserInfoApiModel? ReturnUser { get; set; }

        public Task<LoginResponseApiModel> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.Unauthorized, "Email hoặc mật khẩu không chính xác.");
            }

            return Task.FromResult(new LoginResponseApiModel
            {
                Token = "fake.jwt.token",
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = ReturnUser ?? new UserInfoApiModel
                {
                    AccountId = null,
                    AccountName = "Quản trị viên",
                    AccountEmail = email,
                    AccountRole = null,
                    RoleName = "Admin"
                }
            });
        }
    }

    private static (LoginModel model, DefaultHttpContext httpContext, FakeAuthClientService authService) CreateLoginModel()
    {
        var authService = new FakeAuthClientService();
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        var session = new TestSession();
        httpContext.Session = session;

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        var model = new LoginModel(authService)
        {
            PageContext = pageContext,
            Url = new UrlHelper(actionContext)
        };

        return (model, httpContext, authService);
    }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "test_session_id";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    [Fact]
    public async Task AdminLogin_ShouldSucceed_AndStoreClaimsWithoutFakeAccountId()
    {
        var (model, httpContext, authService) = CreateLoginModel();
        model.Email = "admin@FUNewsManagementSystem.org";
        model.Password = "@@abc123@@";

        authService.ReturnUser = new UserInfoApiModel
        {
            AccountId = null, // Admin KHÔNG có AccountID
            AccountName = "Quản trị viên",
            AccountEmail = "admin@FUNewsManagementSystem.org",
            AccountRole = null,
            RoleName = "Admin"
        };

        var result = await model.OnPostAsync();
        Assert.IsType<RedirectToPageResult>(result);

        // Verify session stored token and role
        Assert.Equal("fake.jwt.token", httpContext.Session.GetString("jwt_token"));
        Assert.Equal("Admin", httpContext.Session.GetString("user_role"));
    }

    [Fact]
    public async Task StaffLogin_ShouldSucceed_AndStoreClaimsWithValidAccountId()
    {
        var (model, httpContext, authService) = CreateLoginModel();
        model.Email = "IsabellaDavid@FUNewsManagement.org";
        model.Password = "@1";

        authService.ReturnUser = new UserInfoApiModel
        {
            AccountId = 3,
            AccountName = "Isabella David",
            AccountEmail = "IsabellaDavid@FUNewsManagement.org",
            AccountRole = 1,
            RoleName = "Staff"
        };

        var result = await model.OnPostAsync();
        Assert.IsType<RedirectToPageResult>(result);

        Assert.Equal("fake.jwt.token", httpContext.Session.GetString("jwt_token"));
        Assert.Equal("Staff", httpContext.Session.GetString("user_role"));
    }

    [Fact]
    public async Task InvalidLogin_ShouldClearPassword_AndDisplayErrorMessage()
    {
        var (model, _, authService) = CreateLoginModel();
        model.Email = "admin@FUNewsManagementSystem.org";
        model.Password = "wrong_password";
        authService.ShouldFail = true;

        var result = await model.OnPostAsync();
        Assert.IsType<PageResult>(result);

        Assert.Equal(string.Empty, model.Password); // Password cleared
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Email hoặc mật khẩu không chính xác", model.ErrorMessage);
    }

    [Fact]
    public async Task AjaxLogin_ValidAdmin_ReturnsJsonSuccess_WithAdminRedirect()
    {
        var (model, httpContext, authService) = CreateLoginModel();
        authService.ReturnUser = new UserInfoApiModel
        {
            AccountId = null,
            AccountName = "Quản trị viên",
            AccountEmail = "admin@FUNewsManagementSystem.org",
            AccountRole = null,
            RoleName = "Admin"
        };

        var request = new LoginRequestApiModel
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };

        var result = await model.OnPostAjaxLoginAsync(request);
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        // Reflection or dynamic access to anonymous object
        var value = jsonResult.Value;
        var successProp = value.GetType().GetProperty("success")?.GetValue(value);
        var redirectProp = value.GetType().GetProperty("redirectUrl")?.GetValue(value);

        Assert.Equal(true, successProp);
        Assert.Equal("/Admin/Accounts", redirectProp);
        Assert.Equal("fake.jwt.token", httpContext.Session.GetString("jwt_token"));
        Assert.Equal("Admin", httpContext.Session.GetString("user_role"));
    }

    [Fact]
    public async Task AjaxLogin_ValidStaff_ReturnsJsonSuccess_WithStaffRedirect()
    {
        var (model, httpContext, authService) = CreateLoginModel();
        authService.ReturnUser = new UserInfoApiModel
        {
            AccountId = 3,
            AccountName = "Isabella David",
            AccountEmail = "IsabellaDavid@FUNewsManagement.org",
            AccountRole = 1,
            RoleName = "Staff"
        };

        var request = new LoginRequestApiModel
        {
            Email = "IsabellaDavid@FUNewsManagement.org",
            Password = "@1"
        };

        var result = await model.OnPostAjaxLoginAsync(request);
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var value = jsonResult.Value;
        var successProp = value.GetType().GetProperty("success")?.GetValue(value);
        var redirectProp = value.GetType().GetProperty("redirectUrl")?.GetValue(value);

        Assert.Equal(true, successProp);
        Assert.Equal("/Staff/News", redirectProp);
        Assert.Equal("fake.jwt.token", httpContext.Session.GetString("jwt_token"));
        Assert.Equal("Staff", httpContext.Session.GetString("user_role"));
    }

    [Fact]
    public async Task AjaxLogin_InvalidCredentials_ReturnsJsonFailure()
    {
        var (model, _, authService) = CreateLoginModel();
        authService.ShouldFail = true;

        var request = new LoginRequestApiModel
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "wrong"
        };

        var result = await model.OnPostAjaxLoginAsync(request);
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var value = jsonResult.Value;
        var successProp = value.GetType().GetProperty("success")?.GetValue(value);
        var messageProp = value.GetType().GetProperty("message")?.GetValue(value)?.ToString();

        Assert.Equal(false, successProp);
        Assert.Contains("Email hoặc mật khẩu không chính xác", messageProp);
    }

    [Fact]
    public async Task AjaxLogin_EmptyFields_ReturnsBadRequestJson()
    {
        var (model, _, _) = CreateLoginModel();

        var request = new LoginRequestApiModel
        {
            Email = "",
            Password = ""
        };

        var result = await model.OnPostAjaxLoginAsync(request);
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var value = jsonResult.Value;
        var successProp = value.GetType().GetProperty("success")?.GetValue(value);
        Assert.Equal(false, successProp);
    }
}
