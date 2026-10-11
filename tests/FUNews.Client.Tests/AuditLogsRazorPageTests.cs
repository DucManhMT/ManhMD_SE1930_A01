using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class AuditLogsRazorPageTests
{
    private class FakeAuditLogClientService : IAuditLogClientService
    {
        public bool ShouldThrow { get; set; }
        public int LastUserId { get; set; }
        public string? LastEntityType { get; set; }
        public int LastPage { get; set; }

        public List<AuditLogApiModel> Logs { get; set; } = new()
        {
            new AuditLogApiModel
            {
                AuditId = 1,
                UserId = 3,
                UserName = "Isabella David",
                Action = "CREATE",
                Entity = "NewsArticle",
                EntityId = "N01",
                BeforeJson = null,
                AfterJson = "{\"Title\":\"New Article\"}",
                Timestamp = DateTime.UtcNow
            },
            new AuditLogApiModel
            {
                AuditId = 2,
                UserId = 3,
                UserName = "Isabella David",
                Action = "UPDATE",
                Entity = "Category",
                EntityId = "1",
                BeforeJson = "{\"Name\":\"Old Name\"}",
                AfterJson = "{\"Name\":\"New Name\"}",
                Timestamp = DateTime.UtcNow
            }
        };

        public Task<AuditLogPagedResponseApiModel> GetAuditLogsAsync(
            int? userId = null,
            string? entityType = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (ShouldThrow)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Lỗi máy chủ khi lấy nhật ký");
            }

            LastUserId = userId ?? 0;
            LastEntityType = entityType;
            LastPage = page;

            return Task.FromResult(new AuditLogPagedResponseApiModel
            {
                Items = Logs,
                TotalCount = Logs.Count,
                Page = page,
                PageSize = pageSize
            });
        }
    }

    private class FakeAccountClientServiceForAudit : IAccountClientService
    {
        public Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<AccountApiModel>
            {
                Count = 1,
                Value = new List<AccountApiModel>
                {
                    new() { AccountId = 3, AccountName = "Isabella David", AccountEmail = "isabella@funews.com", AccountRole = 1 }
                }
            });
        }

        public bool ShouldFailDeleteWithConflict { get; set; }

        public Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default)
        {
            if (ShouldFailDeleteWithConflict)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.Conflict, "Không thể xóa tài khoản này vì đã có bài viết liên kết trong hệ thống!");
            }
            return Task.CompletedTask;
        }

        public Task<AccountApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static AuditLogsModel CreateAuditLogsModel(
        IAuditLogClientService auditLogService,
        IAccountClientService accountService)
    {
        var model = new AuditLogsModel(auditLogService, accountService, NullLogger<AuditLogsModel>.Instance);
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        model.PageContext = new PageContext(actionContext);
        return model;
    }

    [Fact]
    public async Task AuditLogs_OnGetAsync_LoadsAuditLogsAndAccountsSuccessfully()
    {
        var fakeAudit = new FakeAuditLogClientService();
        var fakeAccount = new FakeAccountClientServiceForAudit();
        var pageModel = CreateAuditLogsModel(fakeAudit, fakeAccount);

        pageModel.UserId = 3;
        pageModel.EntityType = "NewsArticle";
        pageModel.PageIndex = 1;

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.NotNull(pageModel.PagedResult);
        Assert.Equal(2, pageModel.PagedResult.Items.Count);
        Assert.Single(pageModel.AccountsList);
        Assert.Equal(3, fakeAudit.LastUserId);
        Assert.Equal("NewsArticle", fakeAudit.LastEntityType);
        Assert.Null(pageModel.ErrorMessage);
    }

    [Fact]
    public async Task AuditLogs_OnGetAsync_WhenServiceFails_SetsErrorMessageGracefully()
    {
        var fakeAudit = new FakeAuditLogClientService { ShouldThrow = true };
        var fakeAccount = new FakeAccountClientServiceForAudit();
        var pageModel = CreateAuditLogsModel(fakeAudit, fakeAccount);

        await pageModel.OnGetAsync(CancellationToken.None);

        Assert.NotNull(pageModel.ErrorMessage);
        Assert.Contains("Không thể tải danh sách nhật ký", pageModel.ErrorMessage);
    }

    [Fact]
    public void BuildAccountsQuery_WithAdminRole_GeneratesCorrectODataQuery()
    {
        var query = ODataFilterHelper.BuildAccountsQuery(null, roleFilter: 0);
        Assert.Contains("accountRole eq 0", query);
    }

    [Fact]
    public async Task Accounts_OnPostDeleteAsync_WhenAccountHasArticles_ReturnsFriendlyConstraintMessage()
    {
        var fakeAccount = new FakeAccountClientServiceForAudit { ShouldFailDeleteWithConflict = true };
        var pageModel = new AccountsModel(fakeAccount, NullLogger<AccountsModel>.Instance);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), new ModelStateDictionary());
        pageModel.PageContext = new PageContext(actionContext);

        var result = await pageModel.OnPostDeleteAsync(3);

        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        var msgProp = type.GetProperty("message");

        Assert.Equal(false, successProp?.GetValue(jsonResult.Value));
        Assert.Equal("Không thể xóa tài khoản này vì đã có bài viết liên kết trong hệ thống!", msgProp?.GetValue(jsonResult.Value)?.ToString());
    }
}
