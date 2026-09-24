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

public class NewsArticleUpdateAndDeleteTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _testArticleIds = new();
    private short? _testInactiveCategoryId;

    public NewsArticleUpdateAndDeleteTests(WebApplicationFactory<Program> factory)
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

        if (_testInactiveCategoryId.HasValue)
        {
            var cat = await db.Categories.FindAsync(_testInactiveCategoryId.Value);
            if (cat != null)
            {
                db.Categories.Remove(cat);
                await db.SaveChangesAsync();
            }
            _testInactiveCategoryId = null;
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

    private async Task<string> SeedInitialArticleAsync(short createdById = 3, short categoryId = 1, List<int>? initialTagIds = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        string articleId = $"TEST_U_{Guid.NewGuid():N}"[..18];
        var article = new NewsArticle
        {
            NewsArticleID = articleId,
            NewsTitle = "Original Article Title for Update Test",
            Headline = "Original Headline for Update Test",
            CreatedDate = new DateTime(2025, 1, 1, 10, 0, 0),
            NewsContent = "Original news content body",
            NewsSource = "Original Source",
            CategoryID = categoryId,
            NewsStatus = true,
            CreatedByID = createdById,
            UpdatedByID = null,
            ModifiedDate = null
        };

        db.NewsArticles.Add(article);

        if (initialTagIds != null && initialTagIds.Count > 0)
        {
            foreach (var tagId in initialTagIds)
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
    public async Task FUN013_AC1_AC2_CreatedByAndDatePreserved_UpdatedByAndModifiedDateAssignedFromToken()
    {
        // AC1: CreatedByID và CreatedDate giữ nguyên sau update
        // AC2: UpdatedByID lấy từ JWT session/claims của user sửa; ModifiedDate cập nhật thời gian server
        string articleId = await SeedInitialArticleAsync(createdById: 3, categoryId: 1, initialTagIds: new() { 1 });

        // Staff 4 (Michael Charlotte) tiến hành sửa bài viết của Staff 3
        var editorClient = CreateStaffClient(accountId: 4, email: "MichaelCharlotte@FUNewsManagement.org", name: "Michael Charlotte");
        var beforeEdit = DateTime.Now.AddSeconds(-2);

        var updatePayload = new UpdateNewsArticleRequestDto
        {
            NewsTitle = "Updated Title by Staff 4",
            Headline = "Updated Headline by Staff 4",
            NewsContent = "Updated content body by Staff 4",
            NewsSource = "Updated Source",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new List<int> { 1, 2 }
        };

        var response = await editorClient.PutAsJsonAsync($"api/news/{articleId}", updatePayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(updated);

        // AC1 Verification: CreatedByID giữ nguyên là 3, CreatedDate giữ nguyên 2025-01-01
        Assert.Equal((short)3, updated.CreatedById);
        Assert.Equal(new DateTime(2025, 1, 1, 10, 0, 0), updated.CreatedDate);

        // AC2 Verification: UpdatedByID là 4 (lấy từ JWT token của editor), ModifiedDate được gán thời gian hiện tại
        Assert.Equal((short)4, updated.UpdatedById);
        Assert.NotNull(updated.ModifiedDate);
        Assert.True(updated.ModifiedDate >= beforeEdit, "ModifiedDate must be updated to recent time");
        Assert.True(updated.ModifiedDate <= DateTime.Now.AddSeconds(2), "ModifiedDate must be within reasonable bounds");
    }

    [Fact]
    public async Task FUN013_AC3_AllowPreservingInactiveCategory_RejectChangingToDifferentInactiveCategory()
    {
        // AC3: Cho phép giữ category inactive cũ; Chặn đổi sang một category inactive khác với 400 Bad Request
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        // Tạo 2 category inactive để test
        var inactiveCat1 = new Category
        {
            CategoryName = "Inactive Cat 1 " + Guid.NewGuid().ToString("N")[..8],
            CategoryDescription = "Inactive category 1 for test",
            IsActive = false
        };
        var inactiveCat2 = new Category
        {
            CategoryName = "Inactive Cat 2 " + Guid.NewGuid().ToString("N")[..8],
            CategoryDescription = "Inactive category 2 for test",
            IsActive = false
        };
        db.Categories.AddRange(inactiveCat1, inactiveCat2);
        await db.SaveChangesAsync();

        _testInactiveCategoryId = inactiveCat1.CategoryID;

        // Seed bài viết vốn thuộc InactiveCat1
        string articleId = await SeedInitialArticleAsync(createdById: 3, categoryId: inactiveCat1.CategoryID, initialTagIds: new() { 1 });

        var staffClient = CreateStaffClient(accountId: 3);

        // 1. Update giữ nguyên inactiveCat1 -> Hợp lệ, trả về 200 OK
        var keepOldInactiveCatPayload = new UpdateNewsArticleRequestDto
        {
            NewsTitle = "Keep Inactive Category Title",
            Headline = "Keep Inactive Headline",
            NewsContent = "Content...",
            NewsSource = "Source",
            CategoryId = inactiveCat1.CategoryID, // Giữ nguyên category inactive cũ
            NewsStatus = true,
            TagIds = new List<int> { 1 }
        };

        var okResponse = await staffClient.PutAsJsonAsync($"api/news/{articleId}", keepOldInactiveCatPayload);
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        // 2. Update cố tình chuyển sang inactiveCat2 -> Bị từ chối 400 Bad Request
        var switchDifferentInactiveCatPayload = new UpdateNewsArticleRequestDto
        {
            NewsTitle = "Switch To Inactive Cat 2",
            Headline = "Invalid Headline",
            NewsContent = "Content...",
            NewsSource = "Source",
            CategoryId = inactiveCat2.CategoryID, // Đổi sang inactive category khác
            NewsStatus = true,
            TagIds = new List<int> { 1 }
        };

        var badResponse = await staffClient.PutAsJsonAsync($"api/news/{articleId}", switchDifferentInactiveCatPayload);
        Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);

        // Dọn dẹp inactiveCat2
        db.Categories.Remove(inactiveCat2);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task FUN013_AC4_AddRemoveTagsAtomic()
    {
        // AC4: Thêm/bớt tags đồng bộ atomic: xóa tags không còn chọn, thêm tags mới
        // Khởi tạo bài viết gắn tag 1 và tag 2
        string articleId = await SeedInitialArticleAsync(createdById: 3, categoryId: 1, initialTagIds: new() { 1, 2 });

        var staffClient = CreateStaffClient(accountId: 3);

        // Cập nhật: bỏ tag 1, giữ tag 2, thêm tag 3 và 4
        var updatePayload = new UpdateNewsArticleRequestDto
        {
            NewsTitle = "Title Atomic Tags",
            Headline = "Headline Atomic Tags",
            NewsContent = "Content Atomic Tags",
            NewsSource = "Source",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new List<int> { 2, 3, 4 }
        };

        var response = await staffClient.PutAsJsonAsync($"api/news/{articleId}", updatePayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(updated);
        Assert.Equal(3, updated.Tags.Count);

        var returnedTagIds = updated.Tags.Select(t => t.TagId).OrderBy(id => id).ToList();
        Assert.Equal(new List<int> { 2, 3, 4 }, returnedTagIds);

        // Kiểm tra trực tiếp trong DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var dbTags = await db.NewsTags.Where(nt => nt.NewsArticleID == articleId).Select(nt => nt.TagID).OrderBy(id => id).ToListAsync();
        Assert.Equal(new List<int> { 2, 3, 4 }, dbTags);
    }

    [Fact]
    public async Task FUN013_AC5_AC6_DeleteRemovesNewsTagsFirst_PreservesSharedTags()
    {
        // AC5: Xóa NewsTag trước khi xóa bài viết, đảm bảo không vi phạm FK
        // AC6: Các Tag trong bảng Tag dùng chung vẫn còn nguyên vẹn sau khi xóa bài viết
        string articleId = await SeedInitialArticleAsync(createdById: 3, categoryId: 1, initialTagIds: new() { 1, 2 });

        // Đảm bảo tag 1 và tag 2 tồn tại trong bảng Tag
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var tag1 = await db.Tags.FindAsync(1);
            var tag2 = await db.Tags.FindAsync(2);
            Assert.NotNull(tag1);
            Assert.NotNull(tag2);
        }

        var staffClient = CreateStaffClient(accountId: 3);

        // Gọi DELETE api/news/{id}
        var deleteResponse = await staffClient.DeleteAsync($"api/news/{articleId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Xác nhận bài viết và NewsTag đã bị xóa khỏi DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            var deletedArticle = await db.NewsArticles.FindAsync(articleId);
            Assert.Null(deletedArticle);

            var remainingNewsTags = await db.NewsTags.Where(nt => nt.NewsArticleID == articleId).ToListAsync();
            Assert.Empty(remainingNewsTags);

            // AC6: Tag 1 và Tag 2 trong bảng Tag vẫn còn nguyên
            var tag1StillExists = await db.Tags.FindAsync(1);
            var tag2StillExists = await db.Tags.FindAsync(2);
            Assert.NotNull(tag1StillExists);
            Assert.NotNull(tag2StillExists);
        }
    }

    [Fact]
    public async Task FUN013_Authorization_OnlyStaffAndAdminCanUpdateAndDelete()
    {
        string articleId = await SeedInitialArticleAsync(createdById: 3, categoryId: 1, initialTagIds: new() { 1 });

        var anonClient = _factory.CreateClient();
        var lecturerClient = CreateLecturerClient();

        var dummyUpdate = new UpdateNewsArticleRequestDto
        {
            NewsTitle = "Unauthorized update",
            Headline = "Unauthorized headline",
            NewsContent = "Unauthorized content",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new() { 1 }
        };

        // 1. Anonymous PUT -> 401
        var anonPut = await anonClient.PutAsJsonAsync($"api/news/{articleId}", dummyUpdate);
        Assert.Equal(HttpStatusCode.Unauthorized, anonPut.StatusCode);

        // 2. Anonymous DELETE -> 401
        var anonDelete = await anonClient.DeleteAsync($"api/news/{articleId}");
        Assert.Equal(HttpStatusCode.Unauthorized, anonDelete.StatusCode);

        // 3. Lecturer PUT -> 403
        var lecturerPut = await lecturerClient.PutAsJsonAsync($"api/news/{articleId}", dummyUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, lecturerPut.StatusCode);

        // 4. Lecturer DELETE -> 403
        var lecturerDelete = await lecturerClient.DeleteAsync($"api/news/{articleId}");
        Assert.Equal(HttpStatusCode.Forbidden, lecturerDelete.StatusCode);
    }
}
