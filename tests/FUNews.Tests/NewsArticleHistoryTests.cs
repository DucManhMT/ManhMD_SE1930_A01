using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class NewsArticleHistoryTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _testArticleIds = new();

    public NewsArticleHistoryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public async Task InitializeAsync()
    {
        await CleanupTestArticlesAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestArticlesAsync();
    }

    private async Task CleanupTestArticlesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        if (_testArticleIds.Count > 0)
        {
            var tags = await db.NewsTags.Where(nt => _testArticleIds.Contains(nt.NewsArticleID)).ToListAsync();
            if (tags.Count > 0)
            {
                db.NewsTags.RemoveRange(tags);
            }

            var articles = await db.NewsArticles.Where(a => _testArticleIds.Contains(a.NewsArticleID)).ToListAsync();
            if (articles.Count > 0)
            {
                db.NewsArticles.RemoveRange(articles);
            }

            await db.SaveChangesAsync();
            _testArticleIds.Clear();
        }
    }

    private HttpClient CreateStaffClient(short accountId, string email, string name)
    {
        var staffUser = new UserInfoDto
        {
            AccountId = accountId,
            AccountEmail = email,
            AccountName = name,
            AccountRole = 1,
            RoleName = "Staff"
        };

        var token = _jwtTokenService.GenerateToken(staffUser, out _);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateLecturerClient()
    {
        var lecturerUser = new UserInfoDto
        {
            AccountId = 2,
            AccountEmail = "OliviaJames@FUNewsManagement.org",
            AccountName = "Olivia James",
            AccountRole = 2,
            RoleName = "Lecturer"
        };

        var token = _jwtTokenService.GenerateToken(lecturerUser, out _);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> SeedArticleAsync(short createdById, string title, string headline, bool newsStatus = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        string articleId = $"TEST_HIST_{Guid.NewGuid():N}"[..18];
        var article = new NewsArticle
        {
            NewsArticleID = articleId,
            NewsTitle = title,
            Headline = headline,
            CreatedDate = DateTime.Now.AddDays(-1),
            NewsContent = "History article content body",
            NewsSource = "Internal",
            CategoryID = 1,
            NewsStatus = newsStatus,
            CreatedByID = createdById,
            UpdatedByID = null,
            ModifiedDate = null
        };

        db.NewsArticles.Add(article);
        await db.SaveChangesAsync();
        _testArticleIds.Add(articleId);
        return articleId;
    }

    [Fact]
    public async Task FUN015_AC1_AC2_AC3_GetMyArticles_ReturnsOnlyArticlesCreatedByCurrentStaff()
    {
        // Tạo bài của Staff 3 (Isabella David) và Staff 4 (Michael Charlotte)
        string articleStaff3 = await SeedArticleAsync(createdById: 3, title: "Staff 3 Unique Article", headline: "Headline for Staff 3");
        string articleStaff4 = await SeedArticleAsync(createdById: 4, title: "Staff 4 Unique Article", headline: "Headline for Staff 4");

        // Client Staff 3
        var clientStaff3 = CreateStaffClient(accountId: 3, email: "IsabellaDavid@FUNewsManagement.org", name: "Isabella David");
        var response3 = await clientStaff3.GetAsync("api/news/mine");
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

        var data3 = await response3.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(data3);
        Assert.NotNull(data3.Value);

        // AC 1, AC 3: Staff 3 chỉ thấy bài của Staff 3, không thấy bài của Staff 4
        Assert.Contains(data3.Value, a => a.NewsArticleId == articleStaff3);
        Assert.DoesNotContain(data3.Value, a => a.NewsArticleId == articleStaff4);
        Assert.All(data3.Value, a => Assert.Equal((short)3, a.CreatedById));

        // Client Staff 4
        var clientStaff4 = CreateStaffClient(accountId: 4, email: "MichaelCharlotte@FUNewsManagement.org", name: "Michael Charlotte");
        var response4 = await clientStaff4.GetAsync("api/news/mine");
        Assert.Equal(HttpStatusCode.OK, response4.StatusCode);

        var data4 = await response4.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(data4);
        Assert.NotNull(data4.Value);

        // AC 1, AC 3: Staff 4 chỉ thấy bài của Staff 4, không thấy bài của Staff 3
        Assert.Contains(data4.Value, a => a.NewsArticleId == articleStaff4);
        Assert.DoesNotContain(data4.Value, a => a.NewsArticleId == articleStaff3);
        Assert.All(data4.Value, a => Assert.Equal((short)4, a.CreatedById));
    }

    [Fact]
    public async Task FUN015_AC2_FilterDoesNotBypassOwnerScope_EvenWithQueryParameters()
    {
        // AC 2: Filter không vượt owner scope kể cả khi cố tình lọc theo CreatedByID của user khác
        string articleStaff4 = await SeedArticleAsync(createdById: 4, title: "Target Article Staff 4", headline: "Secret headline of Staff 4");

        var clientStaff3 = CreateStaffClient(accountId: 3, email: "IsabellaDavid@FUNewsManagement.org", name: "Isabella David");

        // Staff 3 cố gắng inject filter CreatedByID eq 4
        var response = await clientStaff3.GetAsync("api/news/mine?$filter=createdById eq 4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(data);
        Assert.NotNull(data.Value);

        // Kết quả phải là 0 vì điều kiện cơ bản luôn là CreatedByID == 3 AND CreatedByID == 4
        Assert.DoesNotContain(data.Value, a => a.NewsArticleId == articleStaff4);
        Assert.Empty(data.Value);
    }

    [Fact]
    public async Task FUN015_LastModifiedInfo_ReturnsUpdatedByAndModifiedDateWhenArticleWasEdited()
    {
        // Bài viết do Staff 3 tạo
        string articleId = await SeedArticleAsync(createdById: 3, title: "Original Article by Staff 3", headline: "Original Headline");

        // Staff 4 chỉnh sửa bài viết của Staff 3
        var editorClient = CreateStaffClient(accountId: 4, email: "MichaelCharlotte@FUNewsManagement.org", name: "Michael Charlotte");
        var updatePayload = new UpdateNewsArticleRequestDto
        {
            NewsTitle = "Edited Article Title by Staff 4",
            Headline = "Edited Headline by Staff 4",
            NewsContent = "Content after editing by Staff 4",
            NewsSource = "Updated Source",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new List<int>()
        };

        var putResponse = await editorClient.PutAsJsonAsync($"api/news/{articleId}", updatePayload);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Staff 3 mở lại trang lịch sử xem tin do mình tạo
        var ownerClient = CreateStaffClient(accountId: 3, email: "IsabellaDavid@FUNewsManagement.org", name: "Isabella David");
        var historyResponse = await ownerClient.GetAsync($"api/news/mine?$filter=newsArticleId eq '{articleId}'");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var data = await historyResponse.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(data);
        var art = Assert.Single(data.Value);

        // Kiểm tra đúng bài của Staff 3 và hiển thị thông tin người/ngày sửa cuối
        Assert.Equal((short)3, art.CreatedById);
        Assert.Equal((short)4, art.UpdatedById);
        Assert.NotNull(art.LastEditorName);
        Assert.Equal("Michael Charlotte", art.LastEditorName);
        Assert.NotNull(art.ModifiedDate);
    }

    [Fact]
    public async Task FUN015_AC4_NoHistoryTableCreated_QueryDirectlyFromNewsArticles()
    {
        // AC 4: Không tạo bảng history hoặc version history riêng; truy vấn trực tiếp bảng NewsArticle
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        // Kiểm tra database model không có entity nào tên History / ArticleHistory
        var entityNames = db.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();
        Assert.DoesNotContain(entityNames, name => name.Contains("History", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FUN015_Security_AnonymousCannotAccessMyNews_Returns401()
    {
        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("api/news/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FUN015_Security_LecturerCannotAccessMyNews_Returns403()
    {
        var lecturerClient = CreateLecturerClient();
        var response = await lecturerClient.GetAsync("api/news/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
