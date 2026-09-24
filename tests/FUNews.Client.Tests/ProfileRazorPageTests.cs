using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.Staff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class ProfileRazorPageTests
{
    private class FakeAccountClientService : IAccountClientService
    {
        public AccountApiModel ProfileData { get; set; } = new AccountApiModel
        {
            AccountId = 1,
            AccountName = "Staff User One",
            AccountEmail = "staff1@funews.org",
            AccountRole = 1,
            RoleName = "Staff"
        };

        public bool ShouldFailGet { get; set; }
        public bool ShouldFailUpdate { get; set; }
        public string? FailUpdateMessage { get; set; }
        public Dictionary<string, string[]>? FailUpdateErrors { get; set; }

        public bool ShouldFailChangePassword { get; set; }
        public string? FailChangePasswordMessage { get; set; }
        public Dictionary<string, string[]>? FailChangePasswordErrors { get; set; }

        public Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldFailGet)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Không thể tải hồ sơ.");
            }
            return Task.FromResult(ProfileData);
        }

        public Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailUpdate)
            {
                var problem = new ApiProblemDetails
                {
                    Title = "Dữ liệu không hợp lệ",
                    Detail = FailUpdateMessage ?? "Email đã tồn tại",
                    Errors = FailUpdateErrors
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, FailUpdateMessage ?? "Email đã tồn tại", problem);
            }

            ProfileData.AccountName = request.AccountName;
            ProfileData.AccountEmail = request.AccountEmail;
            return Task.FromResult(ProfileData);
        }

        public Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailChangePassword)
            {
                var problem = new ApiProblemDetails
                {
                    Title = "Mật khẩu không chính xác",
                    Detail = FailChangePasswordMessage ?? "Mật khẩu hiện tại không chính xác.",
                    Errors = FailChangePasswordErrors
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, FailChangePasswordMessage ?? "Mật khẩu hiện tại không chính xác.", problem);
            }

            return Task.CompletedTask;
        }

        public Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<AccountApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private static (ProfileModel pageModel, FakeAccountClientService fakeService) CreateProfileModel()
    {
        var fakeService = new FakeAccountClientService();
        var pageModel = new ProfileModel(fakeService, NullLogger<ProfileModel>.Instance);

        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        pageModel.PageContext = new PageContext(actionContext);

        return (pageModel, fakeService);
    }

    [Fact]
    public async Task OnGetAsync_WhenSuccess_LoadsProfile()
    {
        var (pageModel, fakeService) = CreateProfileModel();

        var result = await pageModel.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(pageModel.Profile);
        Assert.Equal("Staff User One", pageModel.Profile.AccountName);
        Assert.Equal("staff1@funews.org", pageModel.Profile.AccountEmail);
        Assert.Null(pageModel.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_WhenApiFails_SetsErrorMessage()
    {
        var (pageModel, fakeService) = CreateProfileModel();
        fakeService.ShouldFailGet = true;

        var result = await pageModel.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(pageModel.Profile);
        Assert.NotNull(pageModel.ErrorMessage);
        Assert.Contains("Không thể tải thông tin", pageModel.ErrorMessage);
    }

    [Fact]
    public async Task OnPostUpdateProfileAsync_WithValidInput_ReturnsSuccessJson()
    {
        var (pageModel, fakeService) = CreateProfileModel();

        var input = new ProfileModel.UpdateProfileInputModel
        {
            AccountName = "New Staff Name",
            AccountEmail = "newemail@funews.org"
        };

        var result = await pageModel.OnPostUpdateProfileAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);

        var propProfile = jsonResult.Value.GetType().GetProperty("profile")?.GetValue(jsonResult.Value) as AccountApiModel;
        Assert.NotNull(propProfile);
        Assert.Equal("New Staff Name", propProfile.AccountName);
        Assert.Equal("newemail@funews.org", propProfile.AccountEmail);
    }

    [Fact]
    public async Task OnPostUpdateProfileAsync_WithInvalidModelState_ReturnsValidationErrors()
    {
        var (pageModel, _) = CreateProfileModel();
        pageModel.ModelState.AddModelError("AccountEmail", "Email không đúng định dạng.");

        var input = new ProfileModel.UpdateProfileInputModel
        {
            AccountName = "Valid Name",
            AccountEmail = "invalid-email"
        };

        var result = await pageModel.OnPostUpdateProfileAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);

        var propErrors = jsonResult.Value.GetType().GetProperty("errors")?.GetValue(jsonResult.Value) as Dictionary<string, string[]>;
        Assert.NotNull(propErrors);
        Assert.True(propErrors.ContainsKey("AccountEmail"));
    }

    [Fact]
    public async Task OnPostUpdateProfileAsync_WhenApiReturnsDuplicateEmail_ReturnsErrorJson()
    {
        var (pageModel, fakeService) = CreateProfileModel();
        fakeService.ShouldFailUpdate = true;
        fakeService.FailUpdateMessage = "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.";
        fakeService.FailUpdateErrors = new Dictionary<string, string[]>
        {
            { "AccountEmail", new[] { "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống." } }
        };

        var input = new ProfileModel.UpdateProfileInputModel
        {
            AccountName = "Staff User",
            AccountEmail = "colliding@funews.org"
        };

        var result = await pageModel.OnPostUpdateProfileAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);

        var propErrors = jsonResult.Value.GetType().GetProperty("errors")?.GetValue(jsonResult.Value) as Dictionary<string, string[]>;
        Assert.NotNull(propErrors);
        Assert.True(propErrors.ContainsKey("AccountEmail"));
    }

    [Fact]
    public async Task OnPostChangePasswordAsync_WithValidInput_ReturnsSuccessJson()
    {
        var (pageModel, _) = CreateProfileModel();

        var input = new ProfileModel.ChangePasswordInputModel
        {
            CurrentPassword = "CurrentPassword@123",
            NewPassword = "BrandNewPassword@123",
            ConfirmPassword = "BrandNewPassword@123"
        };

        var result = await pageModel.OnPostChangePasswordAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);
        var propMessage = jsonResult.Value.GetType().GetProperty("message")?.GetValue(jsonResult.Value) as string;
        Assert.NotNull(propMessage);
        Assert.Contains("Đổi mật khẩu thành công", propMessage);
    }

    [Fact]
    public async Task OnPostChangePasswordAsync_WithIncorrectCurrentPassword_ReturnsErrorJson()
    {
        var (pageModel, fakeService) = CreateProfileModel();
        fakeService.ShouldFailChangePassword = true;
        fakeService.FailChangePasswordMessage = "Mật khẩu hiện tại không chính xác.";
        fakeService.FailChangePasswordErrors = new Dictionary<string, string[]>
        {
            { "CurrentPassword", new[] { "Mật khẩu hiện tại không chính xác." } }
        };

        var input = new ProfileModel.ChangePasswordInputModel
        {
            CurrentPassword = "WrongPassword@123",
            NewPassword = "NewValidPassword@123",
            ConfirmPassword = "NewValidPassword@123"
        };

        var result = await pageModel.OnPostChangePasswordAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);

        var propErrors = jsonResult.Value.GetType().GetProperty("errors")?.GetValue(jsonResult.Value) as Dictionary<string, string[]>;
        Assert.NotNull(propErrors);
        Assert.True(propErrors.ContainsKey("CurrentPassword"));
        Assert.Contains("không chính xác", propErrors["CurrentPassword"][0]);
    }
}
