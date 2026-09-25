using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class NewsArticleDuplicateTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _testArticleIds = new();

    public NewsArticleDuplicateTests(WebApplicationFactory<Program> factory)
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

    private HttpClient CreateStaffClient(short accountId = 3, string email = "IsabellaDavid@FUNewsManagement.org", string name = "Isabella David")
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

    private async Task<string> SeedSourceArticleAsync(short createdById = 3, short categoryId = 1, List<int>? tagIds = null, bool newsStatus = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        string articleId = $"TEST_DUP_{Guid.NewGuid():N}"[..18];
        var article = new NewsArticle
        {
            NewsArticleID = articleId,
            NewsTitle = "Source Article For Duplication Test",
            Headline = "Source Headline for Duplication",
            CreatedDate = new DateTime(2025, 1, 1, 10, 0, 0),
            NewsContent = "Detailed content body of source article for duplication testing.",
            NewsSource = "Internal Source",
            CategoryID = categoryId,
            NewsStatus = newsStatus,
            CreatedByID = createdById,
            UpdatedByID = null,
            ModifiedDate = null
        };

        db.NewsArticles.Add(article);

        if (tagIds != null && tagIds.Count > 0)
        {
            foreach (var tagId in tagIds)
            {
                db.NewsTags.Add(new NewsTag
                {
                    NewsArticleID = articleId,
                    TagID = tagId
                });
            }
        }

        await db.SaveChangesAsync();
        _testArticleIds.Add(articleId);
        return articleId;
    }

    [Fact]
    public async Task FUN014_AC1_AC2_AC3_AC4_DuplicateCreatesNewId_Inactive_CurrentStaffAuthor_NullAudit()
    {
        // Bài viết nguồn do Staff 3 tạo, trạng thái Active (true), ngày tạo 2025-01-01
        string sourceId = await SeedSourceArticleAsync(createdById: 3, categoryId: 1, tagIds: new() { 1 }, newsStatus: true);

        // Staff 4 (Michael Charlotte) thực hiện nhân bản
        var staff4Client = CreateStaffClient(accountId: 4, email: "MichaelCharlotte@FUNewsManagement.org", name: "Michael Charlotte");
        var beforeCall = DateTime.Now.AddSeconds(-2);

        var response = await staff4Client.PostAsync($"api/news/{sourceId}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var duplicatedDto = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(duplicatedDto);

        // Ghi lại ID bài mới để dọn dẹp sau test
        _testArticleIds.Add(duplicatedDto.NewsArticleId);

        // AC 1: ID mới sinh tự động qua SQL sequence, độ dài <= 20 ký tự, không trùng với bài nguồn
        Assert.NotEqual(sourceId, duplicatedDto.NewsArticleId);
        Assert.StartsWith("N", duplicatedDto.NewsArticleId);
        Assert.True(duplicatedDto.NewsArticleId.Length <= 20);

        // AC 2: Trạng thái bản sao luôn là Inactive (false) dù bài gốc là Active (true)
        Assert.False(duplicatedDto.NewsStatus);

        // AC 3: Tác giả là Staff đang đăng nhập (Staff 4), ngày tạo mới (khoảng thời gian hiện tại)
        Assert.Equal((short)4, duplicatedDto.CreatedById);
        Assert.NotNull(duplicatedDto.CreatedDate);
        Assert.True(duplicatedDto.CreatedDate >= beforeCall);
        Assert.True(duplicatedDto.CreatedDate <= DateTime.Now.AddSeconds(2));

        // AC 4: Audit update khởi tạo NULL
        Assert.Null(duplicatedDto.UpdatedById);
        Assert.Null(duplicatedDto.ModifiedDate);

        // Xác minh trực tiếp trong database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var dbArticle = await db.NewsArticles.FirstOrDefaultAsync(a => a.NewsArticleID == duplicatedDto.NewsArticleId);
        Assert.NotNull(dbArticle);
        Assert.False(dbArticle.NewsStatus);
        Assert.Equal((short)4, dbArticle.CreatedByID);
        Assert.Null(dbArticle.UpdatedByID);
        Assert.Null(dbArticle.ModifiedDate);
    }

    [Fact]
    public async Task FUN014_AC5_CopiesAllTagsAtomically()
    {
        // AC 5: Sao chép cả bài viết và danh sách NewsTags phụ thuộc trong database transaction
        var expectedTags = new List<int> { 1, 2 };
        string sourceId = await SeedSourceArticleAsync(createdById: 3, categoryId: 1, tagIds: expectedTags);

        var staffClient = CreateStaffClient(accountId: 3);
        var response = await staffClient.PostAsync($"api/news/{sourceId}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var duplicatedDto = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(duplicatedDto);
        _testArticleIds.Add(duplicatedDto.NewsArticleId);

        // Kiểm tra DTO có đủ tags
        Assert.NotNull(duplicatedDto.Tags);
        var tagIdsInDto = duplicatedDto.Tags.Select(t => t.TagId).OrderBy(t => t).ToList();
        Assert.Equal(expectedTags, tagIdsInDto);

        // Kiểm tra trong database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var dbTags = await db.NewsTags
            .Where(nt => nt.NewsArticleID == duplicatedDto.NewsArticleId)
            .Select(nt => nt.TagID)
            .OrderBy(t => t)
            .ToListAsync();
        Assert.Equal(expectedTags, dbTags);
    }

    [Fact]
    public async Task FUN014_AC7_SourceArticleRemainsUnchanged()
    {
        // AC 7: Toàn bộ dữ liệu, audit, trạng thái của bài viết nguồn giữ nguyên vẹn
        var expectedTags = new List<int> { 1 };
        string sourceId = await SeedSourceArticleAsync(createdById: 3, categoryId: 1, tagIds: expectedTags, newsStatus: true);

        var staffClient = CreateStaffClient(accountId: 4);
        var response = await staffClient.PostAsync($"api/news/{sourceId}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var duplicatedDto = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(duplicatedDto);
        _testArticleIds.Add(duplicatedDto.NewsArticleId);

        // Kiểm tra lại bài viết nguồn trong database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var sourceDb = await db.NewsArticles
            .Include(a => a.NewsTags)
            .FirstOrDefaultAsync(a => a.NewsArticleID == sourceId);

        Assert.NotNull(sourceDb);
        Assert.Equal("Source Article For Duplication Test", sourceDb.NewsTitle);
        Assert.Equal("Source Headline for Duplication", sourceDb.Headline);
        Assert.True(sourceDb.NewsStatus); // Vẫn giữ nguyên trạng thái Active
        Assert.Equal((short)3, sourceDb.CreatedByID); // Tác giả gốc không đổi
        Assert.Equal(new DateTime(2025, 1, 1, 10, 0, 0), sourceDb.CreatedDate); // Ngày tạo gốc không đổi
        Assert.Null(sourceDb.UpdatedByID);
        Assert.Null(sourceDb.ModifiedDate);
        Assert.Single(sourceDb.NewsTags);
        Assert.Equal(1, sourceDb.NewsTags.First().TagID);
    }

    [Fact]
    public async Task FUN014_Security_AnonymousCannotDuplicate_Returns401()
    {
        string sourceId = await SeedSourceArticleAsync();

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.PostAsync($"api/news/{sourceId}/duplicate", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FUN014_Security_LecturerCannotDuplicate_Returns403()
    {
        string sourceId = await SeedSourceArticleAsync();

        var lecturerClient = CreateLecturerClient();
        var response = await lecturerClient.PostAsync($"api/news/{sourceId}/duplicate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FUN014_Validation_NonExistentSourceId_Returns404()
    {
        var staffClient = CreateStaffClient();
        var response = await staffClient.PostAsync("api/news/NON_EXISTENT_ID/duplicate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
