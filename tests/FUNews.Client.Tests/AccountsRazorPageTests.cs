using System.Reflection;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class AccountsRazorPageTests
{
    private class FakeAccountClientService : IAccountClientService
    {
        public List<AccountApiModel> AccountsList { get; set; } = new()
        {
            new AccountApiModel { AccountId = 1, AccountName = "Staff User", AccountEmail = "staff@funews.org", AccountRole = 1, RoleName = "Staff" },
            new AccountApiModel { AccountId = 2, AccountName = "Lecturer User", AccountEmail = "lecturer@funews.org", AccountRole = 2, RoleName = "Lecturer" }
        };

        public bool ShouldFailCreate { get; set; }
        public string? FailErrorMessage { get; set; }
        public Dictionary<string, string[]>? FailValidationErrors { get; set; }

        public Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            var filtered = AccountsList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(odataQuery))
            {
                if (odataQuery.Contains("accountRole eq 1"))
                {
                    filtered = filtered.Where(a => a.AccountRole == 1);
                }
                else if (odataQuery.Contains("accountRole eq 2"))
                {
                    filtered = filtered.Where(a => a.AccountRole == 2);
                }

                if (odataQuery.Contains("contains(accountName,'Staff')"))
                {
                    filtered = filtered.Where(a => a.AccountName != null && a.AccountName.Contains("Staff"));
                }
            }

            var result = filtered.ToList();
            return Task.FromResult(new ODataEnvelope<AccountApiModel>
            {
                Count = result.Count,
                Value = result
            });
        }

        public Task<AccountApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
        {
            var acc = AccountsList.FirstOrDefault(a => a.AccountId == id);
            return Task.FromResult(acc);
        }

        public Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailCreate)
            {
                var problemDetails = new ApiProblemDetails
                {
                    Title = "Dữ liệu không hợp lệ",
                    Detail = FailErrorMessage ?? "Email đã tồn tại",
                    Errors = FailValidationErrors
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, FailErrorMessage ?? "Email đã tồn tại", problemDetails);
            }

            var newAccount = new AccountApiModel
            {
                AccountId = (short)(AccountsList.Count + 1),
                AccountName = request.AccountName,
                AccountEmail = request.AccountEmail,
                AccountRole = request.AccountRole,
                RoleName = request.AccountRole == 1 ? "Staff" : "Lecturer"
            };

            AccountsList.Add(newAccount);
            return Task.FromResult(newAccount);
        }
    }

    private static (AccountsModel pageModel, FakeAccountClientService fakeService) CreateAccountsModel()
    {
        var fakeService = new FakeAccountClientService();
        var pageModel = new AccountsModel(fakeService, NullLogger<AccountsModel>.Instance);

        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        pageModel.PageContext = new PageContext(actionContext);

        return (pageModel, fakeService);
    }

    [Fact]
    public void AccountsModel_ShouldRequire_AdminRole()
    {
        var authAttr = typeof(AccountsModel).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Admin", authAttr.Roles);
    }

    [Fact]
    public async Task OnGetAsync_ShouldLoadAllAccounts_WhenNoFilter()
    {
        var (pageModel, _) = CreateAccountsModel();

        await pageModel.OnGetAsync();

        Assert.NotNull(pageModel.Accounts);
        Assert.Equal(2, pageModel.Accounts.Count);
        Assert.Equal(2, pageModel.TotalCount);
    }

    [Fact]
    public async Task OnGetAsync_WithRoleFilter_ShouldFilterCorrectly()
    {
        var (pageModel, _) = CreateAccountsModel();
        pageModel.RoleFilter = 1; // Staff

        await pageModel.OnGetAsync();

        Assert.Single(pageModel.Accounts);
        Assert.Equal(1, pageModel.Accounts[0].AccountRole);
        Assert.Equal("Staff", pageModel.Accounts[0].RoleName);
    }

    [Fact]
    public async Task OnGetListAsync_ShouldReturnJsonWithAccounts_ForAjax()
    {
        var (pageModel, _) = CreateAccountsModel();

        var result = await pageModel.OnGetListAsync(searchTerm: null, roleFilter: 2);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidInput_ShouldReturnSuccessJson_AndCreatedAccount()
    {
        var (pageModel, fakeService) = CreateAccountsModel();

        var input = new AccountsModel.CreateAccountInputModel
        {
            AccountName = "New Employee",
            AccountEmail = "newemp@funews.org",
            AccountRole = 1,
            AccountPassword = "SecurePassword@123"
        };

        var result = await pageModel.OnPostCreateAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        // Check success in anonymous object via reflection
        var propSuccess = jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);

        var propAccount = jsonResult.Value.GetType().GetProperty("account")?.GetValue(jsonResult.Value) as AccountApiModel;
        Assert.NotNull(propAccount);
        Assert.Equal("New Employee", propAccount.AccountName);
        Assert.Equal("newemp@funews.org", propAccount.AccountEmail);
        Assert.Equal(1, propAccount.AccountRole);
        Assert.Equal(3, fakeService.AccountsList.Count);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithDuplicateEmail_ShouldReturnErrorJson_WithFieldErrors()
    {
        var (pageModel, fakeService) = CreateAccountsModel();
        fakeService.ShouldFailCreate = true;
        fakeService.FailErrorMessage = "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.";
        fakeService.FailValidationErrors = new Dictionary<string, string[]>
        {
            { "AccountEmail", new[] { "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống." } }
        };

        var input = new AccountsModel.CreateAccountInputModel
        {
            AccountName = "Duplicate User",
            AccountEmail = "staff@funews.org",
            AccountRole = 1,
            AccountPassword = "SecurePassword@123"
        };

        var result = await pageModel.OnPostCreateAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var propSuccess = jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);

        var propErrors = jsonResult.Value.GetType().GetProperty("errors")?.GetValue(jsonResult.Value) as Dictionary<string, string[]>;
        Assert.NotNull(propErrors);
        Assert.True(propErrors.ContainsKey("AccountEmail"));
    }

    [Fact]
    public async Task OnPostCreateAsync_WithInvalidRole_ShouldReturnValidationFailure()
    {
        var (pageModel, _) = CreateAccountsModel();

        var input = new AccountsModel.CreateAccountInputModel
        {
            AccountName = "Invalid Role User",
            AccountEmail = "invalidrole@funews.org",
            AccountRole = 5, // Invalid role
            AccountPassword = "SecurePassword@123"
        };

        var result = await pageModel.OnPostCreateAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var propSuccess = jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);
    }
}
