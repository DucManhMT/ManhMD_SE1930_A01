using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class NewsArticleManagementTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;

    public NewsArticleManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public async Task InitializeAsync()
    {
        await CleanupTestNewsArticlesAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestNewsArticlesAsync();
    }

    private async Task CleanupTestNewsArticlesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        // Xóa các NewsTags của bài viết test
        var testNewsTags = await db.NewsTags
            .Where(nt => nt.NewsArticleID.StartsWith("TEST_ART_"))
            .ToListAsync();
        if (testNewsTags.Count > 0)
        {
            db.NewsTags.RemoveRange(testNewsTags);
            await db.SaveChangesAsync();
        }

        // Xóa các NewsArticles test
        var testArticles = await db.NewsArticles
            .Where(a => a.NewsArticleID.StartsWith("TEST_ART_"))
            .ToListAsync();
        if (testArticles.Count > 0)
        {
            db.NewsArticles.RemoveRange(testArticles);
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
    public async Task FUN011_Criterion_01_Role_Scope_Enforced_Before_OData()
    {
        var anonClient = _factory.CreateClient();
        var lecturerClient = CreateLecturerClient();
        var staffClient = CreateStaffClient();

        string activeId = $"TEST_ART_ACT_{Guid.NewGuid():N}"[..18];
        string inactiveId = $"TEST_ART_INA_{Guid.NewGuid():N}"[..18];

        // Setup 1 bài Active và 1 bài Inactive
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            db.NewsArticles.AddRange(
                new NewsArticle
                {
                    NewsArticleID = activeId,
                    NewsTitle = "Active Test Article",
                    Headline = "Active Headline",
                    CreatedDate = DateTime.Now,
                    NewsStatus = true,
                    CategoryID = 1,
                    CreatedByID = 3
                },
                new NewsArticle
                {
                    NewsArticleID = inactiveId,
                    NewsTitle = "Inactive Test Article",
                    Headline = "Inactive Headline",
                    CreatedDate = DateTime.Now,
                    NewsStatus = false,
                    CategoryID = 1,
                    CreatedByID = 3
                }
            );
            await db.SaveChangesAsync();
        }

        // 1. Anonymous gọi OData cố tình filter newsStatus eq false -> Phải nhận danh sách rỗng (không bypass được)
        var anonTryBypassResponse = await anonClient.GetAsync($"api/news?$filter=newsArticleId eq '{inactiveId}' and newsStatus eq false");
        Assert.Equal(HttpStatusCode.OK, anonTryBypassResponse.StatusCode);
        var anonBypassResult = await anonTryBypassResponse.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(anonBypassResult);
        Assert.Empty(anonBypassResult.Value);

        // 2. Anonymous chỉ xem được bài Active
        var anonActiveResponse = await anonClient.GetAsync($"api/news?$filter=newsArticleId eq '{activeId}'");
        Assert.Equal(HttpStatusCode.OK, anonActiveResponse.StatusCode);
        var anonActiveResult = await anonActiveResponse.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(anonActiveResult);
        Assert.Single(anonActiveResult.Value);

        // 3. Lecturer gọi cố xem bài Inactive -> Cũng không bypass được
        var lecturerResponse = await lecturerClient.GetAsync($"api/news?$filter=newsArticleId eq '{inactiveId}'");
        Assert.Equal(HttpStatusCode.OK, lecturerResponse.StatusCode);
        var lecturerResult = await lecturerResponse.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(lecturerResult);
        Assert.Empty(lecturerResult.Value);

        // 4. Staff gọi -> Xem được bài Inactive
        var staffInactiveResponse = await staffClient.GetAsync($"api/news?$filter=newsArticleId eq '{inactiveId}'");
        Assert.Equal(HttpStatusCode.OK, staffInactiveResponse.StatusCode);
        var staffInactiveResult = await staffInactiveResponse.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(staffInactiveResult);
        Assert.Single(staffInactiveResult.Value);
    }

    [Fact]
    public async Task FUN011_Criterion_02_Category_And_Author_Name_Joined_Correctly()
    {
        var staffClient = CreateStaffClient();
        string articleId = $"TEST_ART_JOIN_{Guid.NewGuid():N}"[..18];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            db.NewsArticles.Add(new NewsArticle
            {
                NewsArticleID = articleId,
                NewsTitle = "Join Verification Article",
                Headline = "Checking Category and Author Name",
                CreatedDate = DateTime.Now,
                NewsStatus = true,
                CategoryID = 1,
                CreatedByID = 3
            });
            await db.SaveChangesAsync();
        }

        var response = await staffClient.GetAsync($"api/news?$filter=newsArticleId eq '{articleId}'");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Value);

        var article = result.Value[0];
        Assert.Equal(articleId, article.NewsArticleId);
        Assert.False(string.IsNullOrWhiteSpace(article.CategoryName), "CategoryName must not be empty");
        Assert.False(string.IsNullOrWhiteSpace(article.AuthorName), "AuthorName must not be empty");
        Assert.Equal("Isabella David", article.AuthorName);
    }

    [Fact]
    public async Task FUN011_Criterion_03_Date_Filter_Is_Inclusive()
    {
        var staffClient = CreateStaffClient();
        var targetDate = new DateTime(2026, 5, 20, 15, 30, 00); // Giữa ngày 20/05/2026

        string articleId = $"TEST_ART_DATE_{Guid.NewGuid():N}"[..18];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            db.NewsArticles.Add(new NewsArticle
            {
                NewsArticleID = articleId,
                NewsTitle = "Inclusive Date Test Article",
                Headline = "Testing inclusive bounds",
                CreatedDate = targetDate,
                NewsStatus = true,
                CategoryID = 1,
                CreatedByID = 3
            });
            await db.SaveChangesAsync();
        }

        // Lọc từ 2026-05-20 đến 2026-05-20 (cùng 1 ngày)
        // OData v4 DateTimeOffset literal requires timezone suffix (Z)
        var filterQuery = $"createdDate ge 2026-05-20T00:00:00Z and createdDate lt 2026-05-21T00:00:00Z";
        var response = await staffClient.GetAsync($"api/news?$filter=newsArticleId eq '{articleId}' and {filterQuery}");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"OData query failed with status {response.StatusCode}: {err}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Value);
        Assert.Equal(articleId, result.Value[0].NewsArticleId);
    }

    [Fact]
    public async Task FUN011_Criterion_04_And_05_Sort_Stable_And_Count_Before_Top()
    {
        var staffClient = CreateStaffClient();
        var baseDate = new DateTime(2026, 6, 1, 10, 0, 0);

        string id1 = $"TEST_ART_S1_{Guid.NewGuid():N}"[..18];
        string id2 = $"TEST_ART_S2_{Guid.NewGuid():N}"[..18];
        string id3 = $"TEST_ART_S3_{Guid.NewGuid():N}"[..18];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

            db.NewsArticles.AddRange(
                new NewsArticle
                {
                    NewsArticleID = id1,
                    NewsTitle = "Sort Test 1",
                    Headline = "H1",
                    CreatedDate = baseDate.AddHours(1),
                    NewsStatus = true,
                    CategoryID = 1,
                    CreatedByID = 3
                },
                new NewsArticle
                {
                    NewsArticleID = id2,
                    NewsTitle = "Sort Test 2",
                    Headline = "H2",
                    CreatedDate = baseDate.AddHours(2),
                    NewsStatus = true,
                    CategoryID = 1,
                    CreatedByID = 3
                },
                new NewsArticle
                {
                    NewsArticleID = id3,
                    NewsTitle = "Sort Test 3",
                    Headline = "H3",
                    CreatedDate = baseDate.AddHours(3),
                    NewsStatus = true,
                    CategoryID = 1,
                    CreatedByID = 3
                }
            );
            await db.SaveChangesAsync();
        }

        // AC 5: Gọi $top=2 và $count=true trên 3 bài test
        var filter = $"contains(newsTitle,'Sort Test')";
        var response = await staffClient.GetAsync($"api/news?$filter={filter}&$orderby=createdDate desc,newsArticleId desc&$top=2&$count=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(result);

        // 1. Số lượng items trả về đúng bằng $top=2
        Assert.Equal(2, result.Value.Count);

        // 2. Count phản ánh tổng số bản ghi thỏa mãn filter (3 bài), KHÔNG BỊ giới hạn bởi $top (AC 5)
        Assert.True(result.Count >= 3, $"Count must be at least 3, got {result.Count}");

        // 3. Sắp xếp ổn định giảm dần theo ngày tạo (AC 4)
        Assert.Equal(id3, result.Value[0].NewsArticleId); // mới nhất
        Assert.Equal(id2, result.Value[1].NewsArticleId);
    }
}
