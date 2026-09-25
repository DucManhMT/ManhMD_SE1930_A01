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

public class NewsArticlePublicTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _testArticleIds = new();

    public NewsArticlePublicTests(WebApplicationFactory<Program> factory)
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

    private HttpClient CreateStaffClient()
    {
        var staffUser = new UserInfoDto
        {
            AccountId = 3,
            AccountEmail = "IsabellaDavid@FUNewsManagement.org",
            AccountName = "Isabella David",
            AccountRole = 1,
            RoleName = "Staff"
        };

        var token = _jwtTokenService.GenerateToken(staffUser, out _);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> SeedArticleAsync(bool newsStatus, string title, string headline)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        string articleId = $"TEST_PUB_{Guid.NewGuid():N}"[..18];
        var article = new NewsArticle
        {
            NewsArticleID = articleId,
            NewsTitle = title,
            Headline = headline,
            CreatedDate = DateTime.Now.AddDays(-1),
            NewsContent = "Content for public news article test.",
            NewsSource = "Public Source",
            CategoryID = 1,
            NewsStatus = newsStatus,
            CreatedByID = 3,
            UpdatedByID = null,
            ModifiedDate = null
        };

        db.NewsArticles.Add(article);
        await db.SaveChangesAsync();
        _testArticleIds.Add(articleId);
        return articleId;
    }

    [Fact]
    public async Task FUN016_AC1_NoLoginRequired_AnonymousAndLecturerCanViewPublicListAndDetail()
    {
        string activeId = await SeedArticleAsync(newsStatus: true, title: "Public Active News", headline: "Headline of active news");

        // 1. Anonymous truy cập danh sách và chi tiết
        var anonClient = _factory.CreateClient();
        var listResponse = await anonClient.GetAsync("api/news");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var detailResponse = await anonClient.GetAsync($"api/news/{activeId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var articleDto = await detailResponse.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(articleDto);
        Assert.Equal(activeId, articleDto.NewsArticleId);
        Assert.True(articleDto.NewsStatus);

        // 2. Lecturer truy cập danh sách và chi tiết
        var lecturerClient = CreateLecturerClient();
        var lecturerListResponse = await lecturerClient.GetAsync("api/news");
        Assert.Equal(HttpStatusCode.OK, lecturerListResponse.StatusCode);

        var lecturerDetailResponse = await lecturerClient.GetAsync($"api/news/{activeId}");
        Assert.Equal(HttpStatusCode.OK, lecturerDetailResponse.StatusCode);
    }

    [Fact]
    public async Task FUN016_AC2_InactiveDetail_Returns404NotFoundForAnonymousAndLecturer()
    {
        // AC 2: Bài viết Inactive phải trả 404 cho Anonymous và Lecturer
        string inactiveId = await SeedArticleAsync(newsStatus: false, title: "Secret Inactive News", headline: "Headline of inactive news");

        // Anonymous xem chi tiết bài Inactive -> 404 Not Found
        var anonClient = _factory.CreateClient();
        var anonResponse = await anonClient.GetAsync($"api/news/{inactiveId}");
        Assert.Equal(HttpStatusCode.NotFound, anonResponse.StatusCode);

        // Lecturer xem chi tiết bài Inactive -> 404 Not Found
        var lecturerClient = CreateLecturerClient();
        var lecturerResponse = await lecturerClient.GetAsync($"api/news/{inactiveId}");
        Assert.Equal(HttpStatusCode.NotFound, lecturerResponse.StatusCode);

        // Staff (quản trị) có thể xem bài Inactive qua API
        var staffClient = CreateStaffClient();
        var staffResponse = await staffClient.GetAsync($"api/news/{inactiveId}");
        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
    }

    [Fact]
    public async Task FUN016_AC3_InactiveArticles_NotLeakedInPublicListOrCount()
    {
        // AC 3: Không lộ tin Inactive trong danh sách hay trong @odata.count của người dùng công khai
        string activeId = await SeedArticleAsync(newsStatus: true, title: "Active News Unique", headline: "Active Headline");
        string inactiveId = await SeedArticleAsync(newsStatus: false, title: "Inactive Secret Unique", headline: "Inactive Headline");

        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("api/news?$count=true&$top=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(data);
        Assert.NotNull(data.Value);

        // Chỉ chứa bài Active, tuyệt đối không chứa bài Inactive
        Assert.Contains(data.Value, a => a.NewsArticleId == activeId);
        Assert.DoesNotContain(data.Value, a => a.NewsArticleId == inactiveId);
        Assert.All(data.Value, a => Assert.True(a.NewsStatus));
    }
}
