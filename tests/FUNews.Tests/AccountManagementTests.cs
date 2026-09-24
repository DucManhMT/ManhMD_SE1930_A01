using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class AccountManagementTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AccountManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        _passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    }

    public async Task InitializeAsync()
    {
        await CleanupTestAccountsAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestAccountsAsync();
    }

    private async Task CleanupTestAccountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var testAccounts = await db.SystemAccounts
            .Where(a => a.AccountID > 5)
            .ToListAsync();

        if (testAccounts.Count > 0)
        {
            db.SystemAccounts.RemoveRange(testAccounts);
            await db.SaveChangesAsync();
        }
    }

    private HttpClient CreateClientForRole(string role, short? accountId = null, int? accountRole = null)
    {
        var client = _factory.CreateClient();
        var user = new UserInfoDto
        {
            AccountId = accountId,
            AccountName = $"{role} User",
            AccountEmail = $"{role.ToLowerInvariant()}@test.org",
            AccountRole = accountRole,
            RoleName = role
        };

        var token = _jwtTokenService.GenerateToken(user, out _);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Criterion_06_AccountApi_ShouldEnforce_AdminRoleOnly()
    {
        // 1. Anonymous access should be rejected with 401 Unauthorized
        var anonymousClient = _factory.CreateClient();
        var anonGet = await anonymousClient.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.Unauthorized, anonGet.StatusCode);

        var anonPost = await anonymousClient.PostAsJsonAsync("api/account", new CreateAccountRequestDto
        {
            AccountName = "Anon Test",
            AccountEmail = "anon@test.org",
            AccountRole = 1,
            AccountPassword = "password123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPost.StatusCode);

        // 2. Staff access should be rejected with 403 Forbidden
        var staffClient = CreateClientForRole("Staff", accountId: 3, accountRole: 1);
        var staffGet = await staffClient.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.Forbidden, staffGet.StatusCode);

        var staffPost = await staffClient.PostAsJsonAsync("api/account", new CreateAccountRequestDto
        {
            AccountName = "Staff Test",
            AccountEmail = "stafftest@test.org",
            AccountRole = 1,
            AccountPassword = "password123"
        });
        Assert.Equal(HttpStatusCode.Forbidden, staffPost.StatusCode);

        // 3. Lecturer access should be rejected with 403 Forbidden
        var lecturerClient = CreateClientForRole("Lecturer", accountId: 4, accountRole: 2);
        var lecturerGet = await lecturerClient.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.Forbidden, lecturerGet.StatusCode);

        var lecturerPost = await lecturerClient.PostAsJsonAsync("api/account", new CreateAccountRequestDto
        {
            AccountName = "Lecturer Test",
            AccountEmail = "lecturertest@test.org",
            AccountRole = 2,
            AccountPassword = "password123"
        });
        Assert.Equal(HttpStatusCode.Forbidden, lecturerPost.StatusCode);
    }

    [Fact]
    public async Task Criterion_01_Admin_CanCreateAccount_Successfully_With201Location()
    {
        var adminClient = CreateClientForRole("Admin");
        var uniqueEmail = $"staff_{Guid.NewGuid().ToString("N")[..8]}@funews.edu.vn";
        var rawPassword = "SecurePassword@123";

        var request = new CreateAccountRequestDto
        {
            AccountName = "Nguyễn Văn Test Staff",
            AccountEmail = uniqueEmail,
            AccountRole = 1, // Staff
            AccountPassword = rawPassword
        };

        var response = await adminClient.PostAsJsonAsync("api/account", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Validate Location header
        Assert.NotNull(response.Headers.Location);

        var createdDto = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(createdDto);
        Assert.True(createdDto.AccountId > 0);
        Assert.Equal(request.AccountName, createdDto.AccountName);
        Assert.Equal(uniqueEmail.ToLowerInvariant(), createdDto.AccountEmail?.ToLowerInvariant());
        Assert.Equal(1, createdDto.AccountRole);
        Assert.Equal("Staff", createdDto.RoleName);

        // Acceptance Criteria 2: Hash lưu DB (mật khẩu được băm và lưu an toàn trong cơ sở dữ liệu)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var accountInDb = await db.SystemAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountID == createdDto.AccountId);

        Assert.NotNull(accountInDb);
        Assert.NotNull(accountInDb.AccountPassword);
        Assert.NotEqual(rawPassword, accountInDb.AccountPassword);
        Assert.True(_passwordHasher.IsHashed(accountInDb.AccountPassword));
        Assert.True(_passwordHasher.VerifyPassword(rawPassword, accountInDb.AccountPassword));

        // Acceptance Criteria 3: List không hash (đáp ứng không lộ hash hay password)
        var listResponse = await adminClient.GetAsync("api/account");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var rawJson = await listResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawPassword, rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(accountInDb.AccountPassword, rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Criterion_01_DuplicateEmail_IsRejected_WithBadRequest_AndFieldErrors()
    {
        var adminClient = CreateClientForRole("Admin");
        var existingEmail = $"dup_{Guid.NewGuid().ToString("N")[..8]}@funews.edu.vn";

        // Tạo tài khoản đầu tiên
        var firstAccount = new CreateAccountRequestDto
        {
            AccountName = "First Account",
            AccountEmail = existingEmail,
            AccountRole = 2, // Lecturer
            AccountPassword = "FirstPassword@123"
        };
        var firstResponse = await adminClient.PostAsJsonAsync("api/account", firstAccount);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Cố gắng tạo tài khoản thứ hai với cùng email (thậm chí khác hoa thường hoặc có khoảng trắng)
        var secondAccount = new CreateAccountRequestDto
        {
            AccountName = "Second Account With Same Email",
            AccountEmail = $"  {existingEmail.ToUpperInvariant()}  ",
            AccountRole = 1,
            AccountPassword = "SecondPassword@123"
        };

        var secondResponse = await adminClient.PostAsJsonAsync("api/account", secondAccount);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var problem = await secondResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(
            problem.Errors.ContainsKey("AccountEmail") ||
            problem.Errors.ContainsKey("accountEmail") ||
            problem.Detail != null
        );
    }

    [Fact]
    public async Task Criterion_05_RoleOutside1Or2_IsRejected_WithBadRequest()
    {
        var adminClient = CreateClientForRole("Admin");

        // 1. Role = 0 (Admin role không được phép tạo từ form)
        var roleZeroRequest = new CreateAccountRequestDto
        {
            AccountName = "Invalid Role Zero",
            AccountEmail = $"invalid0_{Guid.NewGuid().ToString("N")[..6]}@funews.edu.vn",
            AccountRole = 0,
            AccountPassword = "Password123"
        };

        var response0 = await adminClient.PostAsJsonAsync("api/account", roleZeroRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response0.StatusCode);

        // 2. Role = 3 (Role không tồn tại)
        var roleThreeRequest = new CreateAccountRequestDto
        {
            AccountName = "Invalid Role Three",
            AccountEmail = $"invalid3_{Guid.NewGuid().ToString("N")[..6]}@funews.edu.vn",
            AccountRole = 3,
            AccountPassword = "Password123"
        };

        var response3 = await adminClient.PostAsJsonAsync("api/account", roleThreeRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response3.StatusCode);

        // 3. Role = -1
        var roleNegativeRequest = new CreateAccountRequestDto
        {
            AccountName = "Invalid Role Negative",
            AccountEmail = $"invalidneg_{Guid.NewGuid().ToString("N")[..6]}@funews.edu.vn",
            AccountRole = -1,
            AccountPassword = "Password123"
        };

        var responseNeg = await adminClient.PostAsJsonAsync("api/account", roleNegativeRequest);
        Assert.Equal(HttpStatusCode.BadRequest, responseNeg.StatusCode);
    }

    [Fact]
    public async Task Criterion_05_MissingRequiredFields_IsRejected_WithValidationErrors()
    {
        var adminClient = CreateClientForRole("Admin");

        var emptyRequest = new CreateAccountRequestDto
        {
            AccountName = "",
            AccountEmail = "invalid-email",
            AccountRole = null,
            AccountPassword = "123" // Quá ngắn
        };

        var response = await adminClient.PostAsJsonAsync("api/account", emptyRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
    }
}
