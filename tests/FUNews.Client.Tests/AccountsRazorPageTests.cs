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

        public bool ShouldFailUpdate { get; set; }
        public string? FailUpdateErrorMessage { get; set; }
        public Dictionary<string, string[]>? FailUpdateValidationErrors { get; set; }

        public bool ShouldFailDelete { get; set; }
        public string? FailDeleteErrorMessage { get; set; }

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

                // M2 regression: support accountEmail eq 'value' filter used by OnGetCheckEmailAsync
                var eqMatch = System.Text.RegularExpressions.Regex.Match(
                    odataQuery, @"accountEmail eq '([^']*)'", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (eqMatch.Success)
                {
                    var targetEmail = eqMatch.Groups[1].Value.ToLowerInvariant();
                    filtered = filtered.Where(a => a.AccountEmail != null
                        && a.AccountEmail.Trim().ToLowerInvariant() == targetEmail);
                }

                var neMatch = System.Text.RegularExpressions.Regex.Match(
                    odataQuery, @"accountId ne (\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (neMatch.Success && short.TryParse(neMatch.Groups[1].Value, out var neId))
                {
                    filtered = filtered.Where(a => a.AccountId != neId);
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

        public Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default)
        {
            if (ShouldFailUpdate)
            {
                var problemDetails = new ApiProblemDetails
                {
                    Title = "Dữ liệu không hợp lệ",
                    Detail = FailUpdateErrorMessage ?? "Lỗi cập nhật",
                    Errors = FailUpdateValidationErrors
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.BadRequest, FailUpdateErrorMessage ?? "Lỗi cập nhật", problemDetails);
            }

            var existing = AccountsList.FirstOrDefault(a => a.AccountId == id);
            if (existing == null)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.NotFound, $"Không tìm thấy tài khoản #{id}");
            }

            existing.AccountName = request.AccountName;
            existing.AccountEmail = request.AccountEmail;
            existing.AccountRole = request.AccountRole;
            existing.RoleName = request.AccountRole == 1 ? "Staff" : "Lecturer";
            return Task.FromResult(existing);
        }

        public Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default)
        {
            if (ShouldFailDelete)
            {
                var problemDetails = new ApiProblemDetails
                {
                    Title = "Xung đột dữ liệu",
                    Detail = FailDeleteErrorMessage ?? "Không thể xóa tài khoản"
                };
                throw new FUNewsApiException(System.Net.HttpStatusCode.Conflict, FailDeleteErrorMessage ?? "Không thể xóa tài khoản", problemDetails);
            }

            var existing = AccountsList.FirstOrDefault(a => a.AccountId == id);
            if (existing == null)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.NotFound, $"Không tìm thấy tài khoản #{id}");
            }

            AccountsList.Remove(existing);
            return Task.CompletedTask;
        }

        public Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            var acc = AccountsList.FirstOrDefault() ?? new AccountApiModel
            {
                AccountId = 1,
                AccountName = "Staff User",
                AccountEmail = "staff@funews.org",
                AccountRole = 1,
                RoleName = "Staff"
            };
            return Task.FromResult(acc);
        }

        public Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default)
        {
            var acc = AccountsList.FirstOrDefault() ?? new AccountApiModel
            {
                AccountId = 1,
                AccountName = request.AccountName,
                AccountEmail = request.AccountEmail,
                AccountRole = 1,
                RoleName = "Staff"
            };
            acc.AccountName = request.AccountName;
            acc.AccountEmail = request.AccountEmail;
            return Task.FromResult(acc);
        }

        public Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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

    // ─── M2 Regression: OnGetCheckEmailAsync ────────────────────────────────

    [Fact]
    public async Task OnGetCheckEmailAsync_WithExistingEmail_ShouldReturnIsDuplicateTrue()
    {
        // Arrange
        var (pageModel, _) = CreateAccountsModel();
        // "staff@funews.org" is pre-seeded in FakeAccountClientService
        const string existingEmail = "staff@funews.org";

        // Act
        var result = await pageModel.OnGetCheckEmailAsync(existingEmail, excludeId: null, CancellationToken.None);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var isDuplicate = jsonResult.Value!.GetType().GetProperty("isDuplicate")?.GetValue(jsonResult.Value);
        var checkFailed = jsonResult.Value.GetType().GetProperty("checkFailed")?.GetValue(jsonResult.Value);
        Assert.Equal(true, isDuplicate);
        Assert.Equal(false, checkFailed);
    }

    [Fact]
    public async Task OnGetCheckEmailAsync_WithNewEmail_ShouldReturnIsDuplicateFalse()
    {
        // Arrange
        var (pageModel, _) = CreateAccountsModel();
        const string newEmail = "brand-new@funews.org";

        // Act
        var result = await pageModel.OnGetCheckEmailAsync(newEmail, excludeId: null, CancellationToken.None);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var isDuplicate = jsonResult.Value!.GetType().GetProperty("isDuplicate")?.GetValue(jsonResult.Value);
        Assert.Equal(false, isDuplicate);
    }

    [Fact]
    public async Task OnGetCheckEmailAsync_WithOwnEmail_WhenExcluded_ShouldReturnIsDuplicateFalse()
    {
        // Arrange
        var (pageModel, _) = CreateAccountsModel();
        // Account 1 has email "staff@funews.org"; when updating account 1, its own email is excluded
        const string ownEmail = "staff@funews.org";

        // Act
        var result = await pageModel.OnGetCheckEmailAsync(ownEmail, excludeId: 1, CancellationToken.None);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var isDuplicate = jsonResult.Value!.GetType().GetProperty("isDuplicate")?.GetValue(jsonResult.Value);
        Assert.Equal(false, isDuplicate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnGetCheckEmailAsync_WithEmptyOrNullEmail_ShouldReturnIsDuplicateFalse(string? email)
    {
        // Arrange
        var (pageModel, _) = CreateAccountsModel();

        // Act
        var result = await pageModel.OnGetCheckEmailAsync(email, excludeId: null, CancellationToken.None);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var isDuplicate = jsonResult.Value!.GetType().GetProperty("isDuplicate")?.GetValue(jsonResult.Value);
        Assert.Equal(false, isDuplicate);
    }

    // ─── FUN-006: Update & Delete Account Razor Page Handlers ─────────────────

    [Fact]
    public async Task OnGetAccountAsync_WithValidId_ShouldReturnAccountJson()
    {
        var (pageModel, _) = CreateAccountsModel();

        var result = await pageModel.OnGetAccountAsync(1, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);

        var propAccount = jsonResult.Value.GetType().GetProperty("account")?.GetValue(jsonResult.Value) as AccountApiModel;
        Assert.NotNull(propAccount);
        Assert.Equal(1, propAccount.AccountId);
        Assert.Equal("Staff User", propAccount.AccountName);
    }

    [Fact]
    public async Task OnGetAccountAsync_WithInvalidId_ShouldReturnNotFoundJson()
    {
        var (pageModel, _) = CreateAccountsModel();

        var result = await pageModel.OnGetAccountAsync(999, CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);
    }

    [Fact]
    public async Task OnPostUpdateAsync_WithValidInput_ShouldReturnSuccessJson_AndUpdatedAccount()
    {
        var (pageModel, fakeService) = CreateAccountsModel();

        var input = new AccountsModel.UpdateAccountInputModel
        {
            AccountId = 1,
            AccountName = "Updated Staff Name",
            AccountEmail = "updatedstaff@funews.org",
            AccountRole = 1
        };

        var result = await pageModel.OnPostUpdateAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);

        var propAccount = jsonResult.Value.GetType().GetProperty("account")?.GetValue(jsonResult.Value) as AccountApiModel;
        Assert.NotNull(propAccount);
        Assert.Equal("Updated Staff Name", propAccount.AccountName);
        Assert.Equal("updatedstaff@funews.org", propAccount.AccountEmail);

        var updatedInDb = fakeService.AccountsList.First(a => a.AccountId == 1);
        Assert.Equal("Updated Staff Name", updatedInDb.AccountName);
    }

    [Fact]
    public async Task OnPostUpdateAsync_WithDuplicateEmail_ShouldReturnErrorJson_WithFieldErrors()
    {
        var (pageModel, fakeService) = CreateAccountsModel();
        fakeService.ShouldFailUpdate = true;
        fakeService.FailUpdateErrorMessage = "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.";
        fakeService.FailUpdateValidationErrors = new Dictionary<string, string[]>
        {
            { "AccountEmail", new[] { "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống." } }
        };

        var input = new AccountsModel.UpdateAccountInputModel
        {
            AccountId = 1,
            AccountName = "Staff User",
            AccountEmail = "lecturer@funews.org", // Collides with Account 2
            AccountRole = 1
        };

        var result = await pageModel.OnPostUpdateAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);

        var propErrors = jsonResult.Value.GetType().GetProperty("errors")?.GetValue(jsonResult.Value) as Dictionary<string, string[]>;
        Assert.NotNull(propErrors);
        Assert.True(propErrors.ContainsKey("AccountEmail"));
    }

    [Fact]
    public async Task OnPostUpdateAsync_WithInvalidRole_ShouldReturnValidationFailure()
    {
        var (pageModel, _) = CreateAccountsModel();

        var input = new AccountsModel.UpdateAccountInputModel
        {
            AccountId = 1,
            AccountName = "Staff User",
            AccountEmail = "staff@funews.org",
            AccountRole = 99 // Invalid role
        };

        var result = await pageModel.OnPostUpdateAsync(input);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithValidId_ShouldRemoveAccount_AndReturnSuccess()
    {
        var (pageModel, fakeService) = CreateAccountsModel();

        var result = await pageModel.OnPostDeleteAsync(2);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);
        Assert.Single(fakeService.AccountsList);
        Assert.DoesNotContain(fakeService.AccountsList, a => a.AccountId == 2);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenReferencedByArticles_ShouldReturnErrorJson()
    {
        var (pageModel, fakeService) = CreateAccountsModel();
        fakeService.ShouldFailDelete = true;
        fakeService.FailDeleteErrorMessage = "Không thể xóa tài khoản vì tài khoản này đã được sử dụng làm tác giả (CreatedBy) của bài viết.";

        var result = await pageModel.OnPostDeleteAsync(1);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);

        var propMessage = jsonResult.Value.GetType().GetProperty("message")?.GetValue(jsonResult.Value) as string;
        Assert.NotNull(propMessage);
        Assert.Contains("CreatedBy", propMessage);
        Assert.Equal(2, fakeService.AccountsList.Count);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithInvalidId_ShouldReturnErrorJson()
    {
        var (pageModel, _) = CreateAccountsModel();

        var result = await pageModel.OnPostDeleteAsync(0);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(false, propSuccess);
        var propMessage = jsonResult.Value.GetType().GetProperty("message")?.GetValue(jsonResult.Value) as string;
        Assert.NotNull(propMessage);
        Assert.Contains("không hợp lệ", propMessage);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithIdFromFormBody_ShouldSucceed()
    {
        var (pageModel, fakeService) = CreateAccountsModel();
        pageModel.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        pageModel.HttpContext.Request.Form = new Microsoft.AspNetCore.Http.FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "id", "2" }
            });

        // Pass 0 in query parameter; id should be resolved from Form
        var result = await pageModel.OnPostDeleteAsync(0);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var propSuccess = jsonResult.Value!.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
        Assert.Equal(true, propSuccess);
        Assert.Single(fakeService.AccountsList);
        Assert.DoesNotContain(fakeService.AccountsList, a => a.AccountId == 2);
    }
}
