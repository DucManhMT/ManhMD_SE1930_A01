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

public class TagManagementTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;

    public TagManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public async Task InitializeAsync()
    {
        await CleanupTestTagsAndArticlesAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestTagsAndArticlesAsync();
    }

    private async Task CleanupTestTagsAndArticlesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        // 1. Remove test NewsTags
        var testNewsTags = await db.NewsTags
            .Where(nt => nt.TagID > 9 || nt.NewsArticleID.StartsWith("TEST_TAG_"))
            .ToListAsync();
        if (testNewsTags.Count > 0)
        {
            db.NewsTags.RemoveRange(testNewsTags);
            await db.SaveChangesAsync();
        }

        // 2. Remove test NewsArticles
        var testArticles = await db.NewsArticles
            .Where(a => a.NewsArticleID.StartsWith("TEST_TAG_"))
            .ToListAsync();
        if (testArticles.Count > 0)
        {
            db.NewsArticles.RemoveRange(testArticles);
            await db.SaveChangesAsync();
        }

        // 3. Remove test Tags (ID > 9)
        var testTags = await db.Tags
            .Where(t => t.TagID > 9)
            .ToListAsync();
        if (testTags.Count > 0)
        {
            db.Tags.RemoveRange(testTags);
            await db.SaveChangesAsync();
        }
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

    private HttpClient CreateLecturerClient()
    {
        var lecturerUser = new UserInfoDto
        {
            AccountId = 2,
            AccountEmail = "EvaSmith@FUNewsManagement.org",
            AccountName = "Eva Smith",
            AccountRole = 2,
            RoleName = "Lecturer"
        };

        var token = _jwtTokenService.GenerateToken(lecturerUser, out _);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task FUN010_Criterion_01_TagName_Required_And_MaxLength_And_Unique_After_Trim()
    {
        var client = CreateStaffClient();

        // 1. Empty / whitespace TagName -> 400 Bad Request
        var emptyNameResponse = await client.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = "    ",
            Note = "Ghi chú hợp lệ"
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyNameResponse.StatusCode);

        // 2. TagName > 50 characters -> 400 Bad Request
        var longNameResponse = await client.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = new string('T', 51),
            Note = "Ghi chú hợp lệ"
        });
        Assert.Equal(HttpStatusCode.BadRequest, longNameResponse.StatusCode);

        // 3. Note > 400 characters -> 400 Bad Request
        var longNoteResponse = await client.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = "ValidTag",
            Note = new string('N', 401)
        });
        Assert.Equal(HttpStatusCode.BadRequest, longNoteResponse.StatusCode);

        // 4. Tạo tag mới với khoảng trắng đầu/cuối
        var baseTagName = $"Tag_{Guid.NewGuid():N}"[..15];
        var createResponse1 = await client.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = $"  {baseTagName}  ",
            Note = "Thẻ kiểm tra trim"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse1.StatusCode);
        var created1 = await createResponse1.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(created1);
        Assert.Equal(baseTagName, created1.TagName); // Đã trim

        // 5. Cố gắng tạo trùng tên (khác chữ hoa chữ thường) -> Bị chặn với 400 Bad Request
        var createResponse2 = await client.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = baseTagName.ToUpper(),
            Note = "Thử tạo trùng tên"
        });
        Assert.Equal(HttpStatusCode.BadRequest, createResponse2.StatusCode);
    }

    [Fact]
    public async Task FUN010_Criterion_02_And_05_Tag_With_Articles_Cannot_Be_Deleted_No_Orphan()
    {
        var client = CreateStaffClient();
        int usedTagId;
        int emptyTagId;

        // 1. Setup DB: Tạo 1 tag rỗng và 1 tag có bài viết
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var maxId = await db.Tags.MaxAsync(t => (int?)t.TagID) ?? 100;

            var usedTag = new Tag
            {
                TagID = Math.Max(100, maxId + 1),
                TagName = $"UsedTag_{Guid.NewGuid():N}"[..20],
                Note = "Tag will be used"
            };
            var emptyTag = new Tag
            {
                TagID = Math.Max(101, maxId + 2),
                TagName = $"EmptyTag_{Guid.NewGuid():N}"[..20],
                Note = "Tag will remain empty"
            };

            db.Tags.AddRange(usedTag, emptyTag);
            await db.SaveChangesAsync();

            usedTagId = usedTag.TagID;
            emptyTagId = emptyTag.TagID;

            var article = new NewsArticle
            {
                NewsArticleID = $"TEST_TAG_{Guid.NewGuid():N}"[..18],
                NewsTitle = "Bài viết gắn thẻ test delete",
                Headline = "Headline test delete",
                CreatedDate = DateTime.Now,
                NewsStatus = true
            };
            db.NewsArticles.Add(article);
            await db.SaveChangesAsync();

            db.NewsTags.Add(new NewsTag
            {
                NewsArticleID = article.NewsArticleID,
                TagID = usedTagId
            });
            await db.SaveChangesAsync();
        }

        // 2. Chặn xóa tag đang có bài viết -> 409 Conflict (AC 2)
        var deleteUsedResponse = await client.DeleteAsync($"api/tag/{usedTagId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteUsedResponse.StatusCode);

        // 3. Xóa tag rỗng -> 204 NoContent (AC 5: No orphan NewsTag)
        var deleteEmptyResponse = await client.DeleteAsync($"api/tag/{emptyTagId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteEmptyResponse.StatusCode);

        // 4. Kiểm tra CSDL: tag rỗng đã bị xóa
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var exists = await db.Tags.AnyAsync(t => t.TagID == emptyTagId);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task FUN010_Criterion_03_And_04_List_Articles_Join_And_Public_Only_Active()
    {
        var staffClient = CreateStaffClient();
        var anonClient = _factory.CreateClient();
        int tagId;

        // 1. Tạo 1 tag có 1 bài Active và 1 bài Inactive
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var maxId = await db.Tags.MaxAsync(t => (int?)t.TagID) ?? 100;

            var tag = new Tag
            {
                TagID = Math.Max(100, maxId + 1),
                TagName = $"JoinTag_{Guid.NewGuid():N}"[..20],
                Note = "Tag with active and inactive articles"
            };
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
            tagId = tag.TagID;

            var activeArticle = new NewsArticle
            {
                NewsArticleID = $"TEST_TAG_ACT_{Guid.NewGuid():N}"[..18],
                NewsTitle = "Bài viết hoạt động (Active)",
                Headline = "Active headline",
                CreatedDate = DateTime.Now,
                NewsStatus = true
            };
            var inactiveArticle = new NewsArticle
            {
                NewsArticleID = $"TEST_TAG_INA_{Guid.NewGuid():N}"[..18],
                NewsTitle = "Bài viết tạm ẩn (Inactive)",
                Headline = "Inactive headline",
                CreatedDate = DateTime.Now,
                NewsStatus = false
            };

            db.NewsArticles.AddRange(activeArticle, inactiveArticle);
            await db.SaveChangesAsync();

            db.NewsTags.Add(new NewsTag { NewsArticleID = activeArticle.NewsArticleID, TagID = tagId });
            db.NewsTags.Add(new NewsTag { NewsArticleID = inactiveArticle.NewsArticleID, TagID = tagId });
            await db.SaveChangesAsync();
        }

        // 2. Staff gọi GET api/tag/{id}/news -> Thấy cả 2 bài viết (AC 3)
        var staffArticlesResponse = await staffClient.GetAsync($"api/tag/{tagId}/news");
        Assert.Equal(HttpStatusCode.OK, staffArticlesResponse.StatusCode);
        var staffArticles = await staffArticlesResponse.Content.ReadFromJsonAsync<List<NewsArticleDto>>();
        Assert.NotNull(staffArticles);
        Assert.Equal(2, staffArticles.Count);

        // 3. Anonymous/Public gọi GET api/tag/{id}/news -> Chỉ thấy 1 bài Active (AC 4)
        var anonArticlesResponse = await anonClient.GetAsync($"api/tag/{tagId}/news");
        Assert.Equal(HttpStatusCode.OK, anonArticlesResponse.StatusCode);
        var anonArticles = await anonArticlesResponse.Content.ReadFromJsonAsync<List<NewsArticleDto>>();
        Assert.NotNull(anonArticles);
        Assert.Single(anonArticles);
        Assert.True(anonArticles[0].NewsStatus);

        // 4. Anonymous GET api/tag/{id} -> ArticleCount không lộ bài Inactive (chỉ đếm 1)
        var anonTagResponse = await anonClient.GetAsync($"api/tag/{tagId}");
        Assert.Equal(HttpStatusCode.OK, anonTagResponse.StatusCode);
        var anonTag = await anonTagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(anonTag);
        Assert.Equal(1, anonTag.ArticleCount);

        // 5. Staff GET api/tag/{id} -> ArticleCount đếm toàn bộ (2)
        var staffTagResponse = await staffClient.GetAsync($"api/tag/{tagId}");
        Assert.Equal(HttpStatusCode.OK, staffTagResponse.StatusCode);
        var staffTag = await staffTagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(staffTag);
        Assert.Equal(2, staffTag.ArticleCount);
    }

    [Fact]
    public async Task FUN010_Criterion_06_Update_Tag_Validations_And_Unique()
    {
        var client = CreateStaffClient();
        int tagId1;
        int tagId2;

        // 1. Tạo 2 tags
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var maxId = await db.Tags.MaxAsync(t => (int?)t.TagID) ?? 100;

            var tag1 = new Tag
            {
                TagID = Math.Max(100, maxId + 1),
                TagName = $"UpdTag1_{Guid.NewGuid():N}"[..20],
                Note = "Tag 1"
            };
            var tag2 = new Tag
            {
                TagID = Math.Max(101, maxId + 2),
                TagName = $"UpdTag2_{Guid.NewGuid():N}"[..20],
                Note = "Tag 2"
            };

            db.Tags.AddRange(tag1, tag2);
            await db.SaveChangesAsync();

            tagId1 = tag1.TagID;
            tagId2 = tag2.TagID;
        }

        // 2. Sửa Tag 1 trùng tên với Tag 2 -> 400 Bad Request
        var updateDupResponse = await client.PutAsJsonAsync($"api/tag/{tagId1}", new UpdateTagRequestDto
        {
            TagName = (await (await client.GetAsync($"api/tag/{tagId2}")).Content.ReadFromJsonAsync<TagDto>())!.TagName!,
            Note = "Thử sửa trùng tên"
        });
        Assert.Equal(HttpStatusCode.BadRequest, updateDupResponse.StatusCode);

        // 3. Sửa Tag 1 với tên mới hợp lệ -> 200 OK
        var newName = $"NewName_{Guid.NewGuid():N}"[..18];
        var updateOkResponse = await client.PutAsJsonAsync($"api/tag/{tagId1}", new UpdateTagRequestDto
        {
            TagName = newName,
            Note = "Ghi chú đã cập nhật"
        });
        Assert.Equal(HttpStatusCode.OK, updateOkResponse.StatusCode);
        var updated = await updateOkResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(updated);
        Assert.Equal(newName, updated.TagName);
        Assert.Equal("Ghi chú đã cập nhật", updated.Note);
    }

    [Fact]
    public async Task FUN010_Role_Authorization_Enforced_For_Tag_Mutation()
    {
        var anonClient = _factory.CreateClient();
        var lecturerClient = CreateLecturerClient();

        // 1. Anonymous POST -> 401 Unauthorized
        var anonPost = await anonClient.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = "AnonTag"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPost.StatusCode);

        // 2. Lecturer POST -> 403 Forbidden
        var lecturerPost = await lecturerClient.PostAsJsonAsync("api/tag", new CreateTagRequestDto
        {
            TagName = "LecturerTag"
        });
        Assert.Equal(HttpStatusCode.Forbidden, lecturerPost.StatusCode);

        // 3. Anonymous PUT -> 401 Unauthorized
        var anonPut = await anonClient.PutAsJsonAsync("api/tag/1", new UpdateTagRequestDto
        {
            TagName = "AnonUpdate"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonPut.StatusCode);

        // 4. Lecturer PUT -> 403 Forbidden
        var lecturerPut = await lecturerClient.PutAsJsonAsync("api/tag/1", new UpdateTagRequestDto
        {
            TagName = "LecturerUpdate"
        });
        Assert.Equal(HttpStatusCode.Forbidden, lecturerPut.StatusCode);

        // 5. Anonymous DELETE -> 401 Unauthorized
        var anonDelete = await anonClient.DeleteAsync("api/tag/1");
        Assert.Equal(HttpStatusCode.Unauthorized, anonDelete.StatusCode);

        // 6. Lecturer DELETE -> 403 Forbidden
        var lecturerDelete = await lecturerClient.DeleteAsync("api/tag/1");
        Assert.Equal(HttpStatusCode.Forbidden, lecturerDelete.StatusCode);
    }

    [Fact]
    public async Task FUN010_Regression_GetByIdWithNewsTagsAsync_AsNoTracking_Does_Not_Track_Entity()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var tagRepo = scope.ServiceProvider.GetRequiredService<FUNews.DataAccess.Repositories.ITagRepository>();

        var maxId = await db.Tags.MaxAsync(t => (int?)t.TagID) ?? 100;
        var tag = new Tag
        {
            TagID = Math.Max(100, maxId + 1),
            TagName = $"NoTrack_{Guid.NewGuid():N}"[..20],
            Note = "Testing AsNoTracking"
        };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        // 1. Fetch with asNoTracking: true -> Entity must NOT be in ChangeTracker
        var fetchedNoTracking = await tagRepo.GetByIdWithNewsTagsAsync(tag.TagID, asNoTracking: true);
        Assert.NotNull(fetchedNoTracking);
        var entryNoTracking = db.Entry(fetchedNoTracking);
        Assert.Equal(EntityState.Detached, entryNoTracking.State);

        // 2. Fetch with asNoTracking: false -> Entity IS tracked
        var fetchedTracked = await tagRepo.GetByIdWithNewsTagsAsync(tag.TagID, asNoTracking: false);
        Assert.NotNull(fetchedTracked);
        var entryTracked = db.Entry(fetchedTracked);
        Assert.NotEqual(EntityState.Detached, entryTracked.State);
    }

    [Fact]
    public async Task FUN010_Regression_Delete_Tag_With_Articles_Returns_ProblemDetails_409()
    {
        var staffClient = CreateStaffClient();
        int usedTagId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var maxId = await db.Tags.MaxAsync(t => (int?)t.TagID) ?? 100;

            var usedTag = new Tag
            {
                TagID = Math.Max(100, maxId + 1),
                TagName = $"RegrDel_{Guid.NewGuid():N}"[..20],
                Note = "Tag for regression delete test"
            };
            db.Tags.Add(usedTag);
            await db.SaveChangesAsync();
            usedTagId = usedTag.TagID;

            var article = new NewsArticle
            {
                NewsArticleID = $"TEST_TAG_REG_{Guid.NewGuid():N}"[..18],
                NewsTitle = "Regression article for delete test",
                Headline = "Regression headline",
                CreatedDate = DateTime.Now,
                NewsStatus = true
            };
            db.NewsArticles.Add(article);
            await db.SaveChangesAsync();

            db.NewsTags.Add(new NewsTag
            {
                NewsArticleID = article.NewsArticleID,
                TagID = usedTagId
            });
            await db.SaveChangesAsync();
        }

        // Action: Cố gắng xóa tag đã có bài viết
        var deleteResponse = await staffClient.DeleteAsync($"api/tag/{usedTagId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        // Assert: ProblemDetails body format theo RFC 7807
        var problem = await deleteResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Contains("không thể xóa", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
