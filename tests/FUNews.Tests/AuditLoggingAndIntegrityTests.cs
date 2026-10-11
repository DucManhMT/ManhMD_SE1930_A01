using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Models;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class AuditLoggingAndIntegrityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuditLoggingAndIntegrityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private class MockUserContext : ICurrentUserContext
    {
        public int? UserId { get; set; } = 99;
        public string? UserName { get; set; } = "staff_tester@funews.com";
    }

    private static FUNewsDbContext CreateTestDbContext(ICurrentUserContext? userContext = null)
    {
        const string connectionString = "Server=localhost;Database=FUNewsManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<FUNewsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FUNewsDbContext(options, userContext ?? new MockUserContext());
    }

    [Fact]
    public async Task AuditLog_ShouldAutomaticallyRecord_CreateUpdateDelete_WithBeforeAfterJson()
    {
        var userContext = new MockUserContext { UserId = 10, UserName = "staff10@funews.com" };
        using var context = CreateTestDbContext(userContext);

        var testTagId = 88881;
        var existingTag = await context.Tags.FindAsync(testTagId);
        if (existingTag != null)
        {
            context.Tags.Remove(existingTag);
            await context.SaveChangesAsync();
        }

        // 1. Thao tác CREATE
        var newTag = new Tag
        {
            TagID = testTagId,
            TagName = "AuditTestTag_Initial",
            Note = "Ghi chú khởi tạo"
        };
        context.Tags.Add(newTag);
        await context.SaveChangesAsync();

        var createLog = await context.AuditLogs
            .OrderByDescending(a => a.AuditID)
            .FirstOrDefaultAsync(a => a.Entity == "Tag" && a.EntityID == testTagId.ToString() && a.Action == "CREATE");

        Assert.NotNull(createLog);
        Assert.Equal("CREATE", createLog.Action);
        Assert.Equal("staff10@funews.com", createLog.UserName);
        Assert.Equal(10, createLog.UserID);
        Assert.Null(createLog.BeforeJSON);
        Assert.NotNull(createLog.AfterJSON);
        Assert.Contains("AuditTestTag_Initial", createLog.AfterJSON);

        // 2. Thao tác UPDATE
        newTag.TagName = "AuditTestTag_Updated";
        await context.SaveChangesAsync();

        var updateLog = await context.AuditLogs
            .OrderByDescending(a => a.AuditID)
            .FirstOrDefaultAsync(a => a.Entity == "Tag" && a.EntityID == testTagId.ToString() && a.Action == "UPDATE");

        Assert.NotNull(updateLog);
        Assert.Equal("UPDATE", updateLog.Action);
        Assert.NotNull(updateLog.BeforeJSON);
        Assert.NotNull(updateLog.AfterJSON);
        Assert.Contains("AuditTestTag_Initial", updateLog.BeforeJSON);
        Assert.Contains("AuditTestTag_Updated", updateLog.AfterJSON);

        // 3. Thao tác DELETE
        context.Tags.Remove(newTag);
        await context.SaveChangesAsync();

        var deleteLog = await context.AuditLogs
            .OrderByDescending(a => a.AuditID)
            .FirstOrDefaultAsync(a => a.Entity == "Tag" && a.EntityID == testTagId.ToString() && a.Action == "DELETE");

        Assert.NotNull(deleteLog);
        Assert.Equal("DELETE", deleteLog.Action);
        Assert.NotNull(deleteLog.BeforeJSON);
        Assert.Null(deleteLog.AfterJSON);
        Assert.Contains("AuditTestTag_Updated", deleteLog.BeforeJSON);

        // Cleanup audit logs của test tag
        var logsToClean = await context.AuditLogs
            .Where(a => a.Entity == "Tag" && a.EntityID == testTagId.ToString())
            .ToListAsync();
        if (logsToClean.Count > 0)
        {
            context.AuditLogs.RemoveRange(logsToClean);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task AuditLog_ShouldMaskPassword_WhenAccountIsAudited()
    {
        var userContext = new MockUserContext { UserId = 1, UserName = "admin@funews.com" };
        using var context = CreateTestDbContext(userContext);

        var testAccountId = (short)9991;
        var existingAccount = await context.SystemAccounts.FindAsync(testAccountId);
        if (existingAccount != null)
        {
            context.SystemAccounts.Remove(existingAccount);
            await context.SaveChangesAsync();
        }

        var account = new SystemAccount
        {
            AccountID = testAccountId,
            AccountName = "AuditTestAccount",
            AccountEmail = "audittest@funews.com",
            AccountRole = 1,
            AccountPassword = "super_secret_raw_password_123"
        };
        context.SystemAccounts.Add(account);
        await context.SaveChangesAsync();

        var auditLog = await context.AuditLogs
            .OrderByDescending(a => a.AuditID)
            .FirstOrDefaultAsync(a => a.Entity == "SystemAccount" && a.EntityID == testAccountId.ToString() && a.Action == "CREATE");

        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.AfterJSON);
        Assert.DoesNotContain("super_secret_raw_password_123", auditLog.AfterJSON);
        Assert.Contains("********", auditLog.AfterJSON);

        // Cleanup
        context.SystemAccounts.Remove(account);
        await context.SaveChangesAsync();

        var logsToClean = await context.AuditLogs
            .Where(a => a.Entity == "SystemAccount" && a.EntityID == testAccountId.ToString())
            .ToListAsync();
        if (logsToClean.Count > 0)
        {
            context.AuditLogs.RemoveRange(logsToClean);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task AuditLogController_Get_ShouldRequireAdminRole()
    {
        // 1. Request không có token -> 401 Unauthorized
        var unauthResponse = await _client.GetAsync("api/auditlog");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthResponse.StatusCode);

        // 2. Request với Admin Token -> 200 OK
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@FUNewsManagementSystem.org",
            Password = "@@abc123@@"
        };
        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResult?.Token);

        var request = new HttpRequestMessage(HttpMethod.Get, "api/auditlog?page=1&pageSize=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuditLogPagedResponseDto>();
        Assert.NotNull(result);
        Assert.True(result.Page >= 1);
        Assert.NotNull(result.Items);
    }
}
