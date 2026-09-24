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

public class NewsArticleCreationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _createdArticleIds = new();

    public NewsArticleCreationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public async Task InitializeAsync()
    {
        await CleanupCreatedArticlesAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupCreatedArticlesAsync();
    }

    private async Task CleanupCreatedArticlesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        if (_createdArticleIds.Count > 0)
        {
            var tags = await db.NewsTags.Where(nt => _createdArticleIds.Contains(nt.NewsArticleID)).ToListAsync();
            if (tags.Count > 0)
            {
                db.NewsTags.RemoveRange(tags);
            }

            var articles = await db.NewsArticles.Where(a => _createdArticleIds.Contains(a.NewsArticleID)).ToListAsync();
            if (articles.Count > 0)
            {
                db.NewsArticles.RemoveRange(articles);
            }

            await db.SaveChangesAsync();
            _createdArticleIds.Clear();
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
    public async Task FUN012_AC1_Actor_And_Date_Server_Assigned()
    {
        // AC 1: CreatedByID lấy từ session/JWT, CreatedDate lấy thời gian server
        var client = CreateStaffClient(accountId: 3, name: "Isabella David");
        var beforeTime = DateTime.Now.AddSeconds(-2);

        var request = new CreateNewsArticleRequestDto
        {
            Headline = "Tiêu đề tóm tắt kiểm thử AC1",
            NewsTitle = "Tiêu đề bài viết AC1",
            NewsContent = "Nội dung bài viết AC1",
            NewsSource = "FU News Center",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new List<int> { 1, 2 }
        };

        var response = await client.PostAsJsonAsync("api/news", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(created);
        Assert.NotEmpty(created.NewsArticleId);
        _createdArticleIds.Add(created.NewsArticleId);

        // Kiểm tra Location header
        Assert.NotNull(response.Headers.Location);

        // Kiểm tra CreatedById do server gán đúng theo JWT
        Assert.Equal((short)3, created.CreatedById);
        Assert.Equal("Isabella David", created.AuthorName);

        // Kiểm tra CreatedDate do server gán thời gian hiện tại
        Assert.NotNull(created.CreatedDate);
        Assert.True(created.CreatedDate >= beforeTime, "CreatedDate must be greater than or equal to beforeTime");
        Assert.True(created.CreatedDate <= DateTime.Now.AddSeconds(2), "CreatedDate must be close to current time");

        // Không có ModifiedDate / UpdatedById khi mới tạo
        Assert.Null(created.UpdatedById);
        Assert.Null(created.ModifiedDate);
    }

    [Fact]
    public async Task FUN012_AC2_Id_Generated_Unique_And_Max20Chars()
    {
        // AC 2: ID <= 20 ký tự, định dạng chuẩn và duy nhất
        var client = CreateStaffClient();

        var req1 = new CreateNewsArticleRequestDto
        {
            Headline = "Bài test sinh mã duy nhất 1",
            CategoryId = 1,
            NewsStatus = true
        };

        var req2 = new CreateNewsArticleRequestDto
        {
            Headline = "Bài test sinh mã duy nhất 2",
            CategoryId = 1,
            NewsStatus = true
        };

        var resp1 = await client.PostAsJsonAsync("api/news", req1);
        var resp2 = await client.PostAsJsonAsync("api/news", req2);

        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);

        var art1 = await resp1.Content.ReadFromJsonAsync<NewsArticleDto>();
        var art2 = await resp2.Content.ReadFromJsonAsync<NewsArticleDto>();

        Assert.NotNull(art1);
        Assert.NotNull(art2);
        _createdArticleIds.Add(art1.NewsArticleId);
        _createdArticleIds.Add(art2.NewsArticleId);

        Assert.StartsWith("N", art1.NewsArticleId);
        Assert.StartsWith("N", art2.NewsArticleId);
        Assert.True(art1.NewsArticleId.Length <= 20);
        Assert.True(art2.NewsArticleId.Length <= 20);
        Assert.NotEqual(art1.NewsArticleId, art2.NewsArticleId);
    }

    [Fact]
    public async Task FUN012_AC3_Validation_Lengths_And_Required_Fields()
    {
        // AC 3: Validation lengths (Headline required <=150, Title <=400, Content <=4000, Source <=400)
        var client = CreateStaffClient();

        // 1. Thiếu headline
        var reqEmptyHeadline = new CreateNewsArticleRequestDto
        {
            Headline = "   ",
            CategoryId = 1
        };
        var respEmptyHeadline = await client.PostAsJsonAsync("api/news", reqEmptyHeadline);
        Assert.Equal(HttpStatusCode.BadRequest, respEmptyHeadline.StatusCode);

        // 2. Headline vượt quá 150 ký tự
        var reqLongHeadline = new CreateNewsArticleRequestDto
        {
            Headline = new string('A', 151),
            CategoryId = 1
        };
        var respLongHeadline = await client.PostAsJsonAsync("api/news", reqLongHeadline);
        Assert.Equal(HttpStatusCode.BadRequest, respLongHeadline.StatusCode);

        // 3. Title vượt quá 400 ký tự
        var reqLongTitle = new CreateNewsArticleRequestDto
        {
            Headline = "Tóm tắt hợp lệ",
            NewsTitle = new string('B', 401),
            CategoryId = 1
        };
        var respLongTitle = await client.PostAsJsonAsync("api/news", reqLongTitle);
        Assert.Equal(HttpStatusCode.BadRequest, respLongTitle.StatusCode);

        // 4. Source vượt quá 400 ký tự
        var reqLongSource = new CreateNewsArticleRequestDto
        {
            Headline = "Tóm tắt hợp lệ",
            NewsSource = new string('C', 401),
            CategoryId = 1
        };
        var respLongSource = await client.PostAsJsonAsync("api/news", reqLongSource);
        Assert.Equal(HttpStatusCode.BadRequest, respLongSource.StatusCode);

        // 5. Content vượt quá 4000 ký tự
        var reqLongContent = new CreateNewsArticleRequestDto
        {
            Headline = "Tóm tắt hợp lệ",
            NewsContent = new string('D', 4001),
            CategoryId = 1
        };
        var respLongContent = await client.PostAsJsonAsync("api/news", reqLongContent);
        Assert.Equal(HttpStatusCode.BadRequest, respLongContent.StatusCode);
    }

    [Fact]
    public async Task FUN012_AC4_Category_Active_Enforced()
    {
        // AC 4: Category phải tồn tại và đang hoạt động (IsActive == true)
        var client = CreateStaffClient();

        // 1. Tạo category tạm thời có IsActive = false
        short inactiveCategoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var inactiveCat = new Category
            {
                CategoryName = $"InactiveCat_{Guid.NewGuid():N}"[..20],
                CategoryDescription = "Mô tả tạm ẩn",
                IsActive = false
            };
            db.Categories.Add(inactiveCat);
            await db.SaveChangesAsync();
            inactiveCategoryId = inactiveCat.CategoryID;
        }

        try
        {
            // Cố gắng tạo bài viết gắn vào category inactive
            var reqInactive = new CreateNewsArticleRequestDto
            {
                Headline = "Bài viết với category inactive",
                CategoryId = inactiveCategoryId
            };
            var respInactive = await client.PostAsJsonAsync("api/news", reqInactive);
            Assert.Equal(HttpStatusCode.BadRequest, respInactive.StatusCode);

            // Cố gắng tạo bài viết gắn vào category không tồn tại
            var reqNotFound = new CreateNewsArticleRequestDto
            {
                Headline = "Bài viết với category không tồn tại",
                CategoryId = 9999
            };
            var respNotFound = await client.PostAsJsonAsync("api/news", reqNotFound);
            Assert.Equal(HttpStatusCode.BadRequest, respNotFound.StatusCode);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var cat = await db.Categories.FindAsync(inactiveCategoryId);
            if (cat != null)
            {
                db.Categories.Remove(cat);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task FUN012_AC5_Tags_Exist_And_Distinct()
    {
        // AC 5: Các tagIds tồn tại và được deduplicate (không trùng lặp liên kết)
        var client = CreateStaffClient();

        var request = new CreateNewsArticleRequestDto
        {
            Headline = "Kiểm tra deduplicate tags",
            CategoryId = 1,
            NewsStatus = true,
            TagIds = new List<int> { 1, 1, 2, 2, 1 } // Trùng lặp cố tình
        };

        var response = await client.PostAsJsonAsync("api/news", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<NewsArticleDto>();
        Assert.NotNull(created);
        _createdArticleIds.Add(created.NewsArticleId);

        // Kiểm tra trong DB rằng NewsTag chỉ có đúng 2 bản ghi (Tag 1 và Tag 2), không bị trùng lặp
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var newsTags = await db.NewsTags
            .Where(nt => nt.NewsArticleID == created.NewsArticleId)
            .ToListAsync();

        Assert.Equal(2, newsTags.Count);
        Assert.Contains(newsTags, nt => nt.TagID == 1);
        Assert.Contains(newsTags, nt => nt.TagID == 2);
    }

    [Fact]
    public async Task FUN012_AC6_Atomic_Insert_Rollback_When_Tag_Invalid()
    {
        // AC 6: Một tag sai rollback toàn bộ — không lưu bài viết và không có orphan NewsTag
        var client = CreateStaffClient();

        var request = new CreateNewsArticleRequestDto
        {
            Headline = "Bài viết có tag sai phải rollback",
            NewsTitle = "Rollback Test Title",
            CategoryId = 1,
            TagIds = new List<int> { 1, 999999 } // 1 tồn tại, 999999 không tồn tại
        };

        var response = await client.PostAsJsonAsync("api/news", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Xác minh trong DB không có bài viết nào với tiêu đề này được lưu lại
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var articleExists = await db.NewsArticles.AnyAsync(a => a.Headline == "Bài viết có tag sai phải rollback");
        Assert.False(articleExists, "Article must NOT be saved when tag validation fails.");
    }

    [Fact]
    public async Task FUN012_AC7_Role_Enforcement_NonStaff_Cannot_Create()
    {
        // Phân quyền: Anonymous trả về 401, Lecturer trả về 403
        var anonymousClient = _factory.CreateClient();
        var lecturerClient = CreateLecturerClient();

        var request = new CreateNewsArticleRequestDto
        {
            Headline = "Cố tình tạo bài không phải Staff",
            CategoryId = 1
        };

        var respAnon = await anonymousClient.PostAsJsonAsync("api/news", request);
        Assert.Equal(HttpStatusCode.Unauthorized, respAnon.StatusCode);

        var respLecturer = await lecturerClient.PostAsJsonAsync("api/news", request);
        Assert.Equal(HttpStatusCode.Forbidden, respLecturer.StatusCode);
    }
}
