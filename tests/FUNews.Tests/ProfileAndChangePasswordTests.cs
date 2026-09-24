using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Security;
using FUNews.BusinessLogic.Services;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class ProfileAndChangePasswordTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileAndChangePasswordTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        _passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    }

    public async Task InitializeAsync()
    {
        await CleanupAndSetupTestAccountsAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestAccountsAsync();
    }

    private async Task CleanupTestAccountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var testAccounts = await db.SystemAccounts.Where(a => a.AccountID > 5).ToListAsync();
        if (testAccounts.Count > 0)
        {
            db.SystemAccounts.RemoveRange(testAccounts);
            await db.SaveChangesAsync();
        }
    }

    private async Task CleanupAndSetupTestAccountsAsync()
    {
        await CleanupTestAccountsAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        db.SystemAccounts.Add(new SystemAccount
        {
            AccountID = 101,
            AccountName = "Staff User One",
            AccountEmail = "staff101@funews.org",
            AccountRole = 1,
            AccountPassword = _passwordHasher.HashPassword("Staff101Password@123")
        });

        db.SystemAccounts.Add(new SystemAccount
        {
            AccountID = 102,
            AccountName = "Staff User Two",
            AccountEmail = "staff102@funews.org",
            AccountRole = 1,
            AccountPassword = _passwordHasher.HashPassword("Staff102Password@123")
        });

        await db.SaveChangesAsync();
    }

    private HttpClient CreateClientForUser(string role, short? accountId = null, int? accountRole = null, string? email = null)
    {
        var client = _factory.CreateClient();
        var user = new UserInfoDto
        {
            AccountId = accountId,
            AccountName = $"{role} User",
            AccountEmail = email ?? $"{role.ToLowerInvariant()}@funews.org",
            AccountRole = accountRole,
            RoleName = role
        };

        var token = _jwtTokenService.GenerateToken(user, out _);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Criterion_01_Profile_CanOnlyBeAccessed_ByAuthenticatedUser_ForThemselves()
    {
        // 1. Anonymous access is rejected with 401 Unauthorized
        var anonymousClient = _factory.CreateClient();
        var anonGet = await anonymousClient.GetAsync("api/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonGet.StatusCode);

        var anonPut = await anonymousClient.PutAsJsonAsync("api/account/me", new UpdateProfileRequestDto
        {
            AccountName = "Hacker",
            AccountEmail = "hacker@test.org"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPut.StatusCode);

        var anonPw = await anonymousClient.PostAsJsonAsync("api/account/me/change-password", new ChangePasswordRequestDto
        {
            CurrentPassword = "123",
            NewPassword = "456",
            ConfirmPassword = "456"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPw.StatusCode);

        // 2. Admin cannot access /account/me because Admin is a configuration-only user with no SystemAccount
        var adminClient = CreateClientForUser("Admin");
        var adminGet = await adminClient.GetAsync("api/account/me");
        Assert.Equal(HttpStatusCode.Forbidden, adminGet.StatusCode);

        // 3. Staff 101 gets only their own profile
        var staff1Client = CreateClientForUser("Staff", accountId: 101, accountRole: 1, email: "staff101@funews.org");
        var staff1Get = await staff1Client.GetAsync("api/account/me?id=102"); // Passing external query id should be ignored!
        Assert.Equal(HttpStatusCode.OK, staff1Get.StatusCode);

        var profile = await staff1Get.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(profile);
        Assert.Equal(101, profile.AccountId);
        Assert.Equal("Staff User One", profile.AccountName);
        Assert.Equal("staff101@funews.org", profile.AccountEmail);
    }

    [Fact]
    public async Task Criterion_02_UpdateProfile_RoleAndId_CannotBeChanged_AndRejectsDuplicateEmail()
    {
        var staff1Client = CreateClientForUser("Staff", accountId: 101, accountRole: 1, email: "staff101@funews.org");

        // 1. Updating name and valid email
        var updateRequest = new UpdateProfileRequestDto
        {
            AccountName = "Staff User One Updated",
            AccountEmail = "staff101.updated@funews.org"
        };

        var response = await staff1Client.PutAsJsonAsync("api/account/me", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedDto = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(updatedDto);
        Assert.Equal(101, updatedDto.AccountId);
        Assert.Equal("Staff User One Updated", updatedDto.AccountName);
        Assert.Equal("staff101.updated@funews.org", updatedDto.AccountEmail);
        Assert.Equal(1, updatedDto.AccountRole); // Role remains Staff (1)

        // Verify in DB directly
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var inDb = await db.SystemAccounts.FirstAsync(a => a.AccountID == 101);
            Assert.Equal("Staff User One Updated", inDb.AccountName);
            Assert.Equal("staff101.updated@funews.org", inDb.AccountEmail);
            Assert.Equal(1, inDb.AccountRole); // Role NEVER changed
        }

        // 2. Colliding with Staff 102's email must be rejected with 400 Bad Request
        var duplicateEmailRequest = new UpdateProfileRequestDto
        {
            AccountName = "Staff User One",
            AccountEmail = "staff102@funews.org" // Collides with Account 102
        };

        var dupResponse = await staff1Client.PutAsJsonAsync("api/account/me", duplicateEmailRequest);
        Assert.Equal(HttpStatusCode.BadRequest, dupResponse.StatusCode);

        var problem = await dupResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("AccountEmail"));
    }

    [Fact]
    public async Task Criterion_03_ChangePassword_WithIncorrectCurrentPassword_RejectsAndPreservesPassword()
    {
        var staff1Client = CreateClientForUser("Staff", accountId: 101, accountRole: 1, email: "staff101@funews.org");

        string originalHash;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var inDb = await db.SystemAccounts.FirstAsync(a => a.AccountID == 101);
            originalHash = inDb.AccountPassword!;
        }

        // Send incorrect current password
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongPassword@999",
            NewPassword = "BrandNewPassword@123",
            ConfirmPassword = "BrandNewPassword@123"
        };

        var response = await staff1Client.PostAsJsonAsync("api/account/me/change-password", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("CurrentPassword"));
        Assert.Contains("không chính xác", problem.Errors["CurrentPassword"][0]);

        // Verify password hash in DB was NOT changed
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var inDb = await db.SystemAccounts.FirstAsync(a => a.AccountID == 101);
            Assert.Equal(originalHash, inDb.AccountPassword);
        }
    }

    [Fact]
    public async Task Criterion_04_ChangePassword_WithValidCredentials_Succeeds_AndEnablesNewLogin()
    {
        var staff1Client = CreateClientForUser("Staff", accountId: 101, accountRole: 1, email: "staff101@funews.org");

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "Staff101Password@123",
            NewPassword = "NewlyUpdatedPassword@2026",
            ConfirmPassword = "NewlyUpdatedPassword@2026"
        };

        var response = await staff1Client.PostAsJsonAsync("api/account/me/change-password", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify login with new password succeeds
        using (var scope = _factory.Services.CreateScope())
        {
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            // Old password must now fail
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                authService.LoginAsync(new LoginRequestDto
                {
                    Email = "staff101@funews.org",
                    Password = "Staff101Password@123"
                }));

            // New password must succeed
            var loginResult = await authService.LoginAsync(new LoginRequestDto
            {
                Email = "staff101@funews.org",
                Password = "NewlyUpdatedPassword@2026"
            });

            Assert.NotNull(loginResult);
            Assert.NotNull(loginResult.Token);
            Assert.Equal("staff101@funews.org", loginResult.User.AccountEmail);
        }
    }

    [Fact]
    public async Task Criterion_05_ChangePassword_ValidationRules_EnforcesLengthAndConfirmMatch()
    {
        var staff1Client = CreateClientForUser("Staff", accountId: 101, accountRole: 1, email: "staff101@funews.org");

        // 1. Password shorter than 6 characters
        var shortPwRequest = new ChangePasswordRequestDto
        {
            CurrentPassword = "Staff101Password@123",
            NewPassword = "123",
            ConfirmPassword = "123"
        };

        var shortResponse = await staff1Client.PostAsJsonAsync("api/account/me/change-password", shortPwRequest);
        Assert.Equal(HttpStatusCode.BadRequest, shortResponse.StatusCode);

        // 2. Mismatch between NewPassword and ConfirmPassword
        var mismatchRequest = new ChangePasswordRequestDto
        {
            CurrentPassword = "Staff101Password@123",
            NewPassword = "ValidPassword@123",
            ConfirmPassword = "DifferentPassword@456"
        };

        var mismatchResponse = await staff1Client.PostAsJsonAsync("api/account/me/change-password", mismatchRequest);
        Assert.Equal(HttpStatusCode.BadRequest, mismatchResponse.StatusCode);
    }
}
