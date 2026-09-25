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

public class NewsArticleReportTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _testArticleIds = new();
    private readonly List<short> _testCategoryIds = new();

    public NewsArticleReportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        using var scope = _factory.Services.CreateScope();
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
        }

        if (_testCategoryIds.Count > 0)
        {
            var cats = await db.Categories.Where(c => _testCategoryIds.Contains(c.CategoryID)).ToListAsync();
            if (cats.Count > 0)
            {
                db.Categories.RemoveRange(cats);
            }
        }

        await db.SaveChangesAsync();
        _testArticleIds.Clear();
        _testCategoryIds.Clear();
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

    private async Task<short> SeedCategoryAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        var cat = new Category
        {
            CategoryName = name,
            CategoryDescription = "Mô tả test",
            IsActive = true
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        _testCategoryIds.Add(cat.CategoryID);
        return cat.CategoryID;
    }

    private async Task<NewsArticle> SeedArticleAsync(
        string id,
        string title,
        short? categoryId,
        bool status,
        short authorId,
        DateTime createdDate,
        short? updatedById = null,
        DateTime? modifiedDate = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        var article = new NewsArticle
        {
            NewsArticleID = id,
            NewsTitle = title,
            Headline = $"Headline cho {title}",
            NewsContent = $"Content cho {title}",
            NewsSource = "FPT Education",
            CategoryID = categoryId,
            NewsStatus = status,
            CreatedByID = authorId,
            CreatedDate = createdDate,
            UpdatedByID = updatedById,
            ModifiedDate = modifiedDate
        };

        db.NewsArticles.Add(article);
        await db.SaveChangesAsync();
        _testArticleIds.Add(id);
        return article;
    }

    [Fact]
    public async Task Criterion_06_ReportApi_ShouldEnforce_AdminRoleOnly()
    {
        // 1. Anonymous access should return 401 Unauthorized
        var anonClient = _factory.CreateClient();
        var anonResponse = await anonClient.GetAsync("api/report");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        // 2. Staff access should return 403 Forbidden (Test Plan T02)
        var staffClient = CreateClientForRole("Staff", accountId: 3, accountRole: 1);
        var staffResponse = await staffClient.GetAsync("api/report");
        Assert.Equal(HttpStatusCode.Forbidden, staffResponse.StatusCode);

        // 3. Lecturer access should return 403 Forbidden
        var lecturerClient = CreateClientForRole("Lecturer", accountId: 4, accountRole: 2);
        var lecturerResponse = await lecturerClient.GetAsync("api/report");
        Assert.Equal(HttpStatusCode.Forbidden, lecturerResponse.StatusCode);

        // 4. Admin access should return 200 OK
        var adminClient = CreateClientForRole("Admin");
        var adminResponse = await adminClient.GetAsync("api/report");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Criterion_01_Validation_StartDateGreaterThanEndDate_Returns400BadRequest()
    {
        var adminClient = CreateClientForRole("Admin");
        var startDate = "2026-03-25";
        var endDate = "2026-03-20";

        var response = await adminClient.GetAsync($"api/report?startDate={startDate}&endDate={endDate}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Khoảng ngày không hợp lệ", content);
    }

    [Fact]
    public async Task Criterion_02_InclusiveEndDate_CoversEntireEndOfDay_And_ExcludesOutsideRange()
    {
        var adminClient = CreateClientForRole("Admin");
        var catId = await SeedCategoryAsync("Report Boundary Category");

        var testBase = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Local);
        var startDate = testBase.Date; // 2026-06-15
        var endDate = testBase.AddDays(2).Date; // 2026-06-17

        // 1. Trước startDate -> Bị loại
        await SeedArticleAsync("RPT_BOUND_01", "Bài trước startDate", catId, true, 2, startDate.AddSeconds(-1));

        // 2. Đúng startDate 00:00:00 -> Được nhận
        await SeedArticleAsync("RPT_BOUND_02", "Bài đúng startDate", catId, true, 2, startDate);

        // 3. Cuối ngày endDate 23:59:59 -> Phải được nhận (Inclusive EndDate)
        await SeedArticleAsync("RPT_BOUND_03", "Bài cuối ngày endDate", catId, true, 2, endDate.AddHours(23).AddMinutes(59).AddSeconds(59));

        // 4. Sau endDate (qua ngày hôm sau 00:00:00) -> Bị loại
        await SeedArticleAsync("RPT_BOUND_04", "Bài sau endDate", catId, true, 2, endDate.AddDays(1));

        var response = await adminClient.GetAsync($"api/report?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<NewsReportDto>();
        Assert.NotNull(report);

        var returnedIds = report.Details.Select(a => a.NewsArticleId).ToList();
        Assert.Contains("RPT_BOUND_02", returnedIds);
        Assert.Contains("RPT_BOUND_03", returnedIds);
        Assert.DoesNotContain("RPT_BOUND_01", returnedIds);
        Assert.DoesNotContain("RPT_BOUND_04", returnedIds);
    }

    [Fact]
    public async Task Criterion_03_04_05_AggregateTotals_SortDesc_NullableLeftJoin()
    {
        var adminClient = CreateClientForRole("Admin");
        var catId = await SeedCategoryAsync("Report Category Test");

        var startDate = new DateTime(2026, 7, 1);
        var endDate = new DateTime(2026, 7, 10);

        // Bài 1: Active, có Category, có Editor
        await SeedArticleAsync("RPT_DETAIL_01", "Bài viết A", catId, true, 2, new DateTime(2026, 7, 2, 10, 0, 0), updatedById: 3, modifiedDate: new DateTime(2026, 7, 3, 14, 0, 0));

        // Bài 2: Inactive, KHÔNG có Category (null CategoryID), Chưa từng sửa (null UpdatedByID, null ModifiedDate)
        await SeedArticleAsync("RPT_DETAIL_02", "Bài viết B không có mục", null, false, 2, new DateTime(2026, 7, 5, 12, 0, 0), updatedById: null, modifiedDate: null);

        // Bài 3: Active, mới nhất trong khoảng
        await SeedArticleAsync("RPT_DETAIL_03", "Bài viết C mới nhất", catId, true, 2, new DateTime(2026, 7, 8, 9, 0, 0));

        var response = await adminClient.GetAsync($"api/report?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&groupBy=category");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<NewsReportDto>();
        Assert.NotNull(report);

        // AC 3: Aggregate totals
        Assert.Equal(3, report.TotalArticles);
        Assert.Equal(2, report.TotalActive);
        Assert.Equal(1, report.TotalInactive);

        // AC 4: Sort CreatedDate desc
        Assert.Equal(3, report.Details.Count);
        Assert.Equal("RPT_DETAIL_03", report.Details[0].NewsArticleId); // 2026-07-08
        Assert.Equal("RPT_DETAIL_02", report.Details[1].NewsArticleId); // 2026-07-05
        Assert.Equal("RPT_DETAIL_01", report.Details[2].NewsArticleId); // 2026-07-02

        // AC 5: Nullable left join check
        var nullCatArticle = report.Details.First(a => a.NewsArticleId == "RPT_DETAIL_02");
        Assert.Equal("Không phân loại", nullCatArticle.CategoryName);
        Assert.Equal("Chưa chỉnh sửa", nullCatArticle.LastEditorName);
        Assert.Null(nullCatArticle.ModifiedDate);

        var editedArticle = report.Details.First(a => a.NewsArticleId == "RPT_DETAIL_01");
        Assert.Equal("Report Category Test", editedArticle.CategoryName);
        Assert.NotEqual("Chưa chỉnh sửa", editedArticle.LastEditorName);
        Assert.NotNull(editedArticle.ModifiedDate);
    }

    [Fact]
    public async Task Criterion_03_Grouping_ByAuthor_And_ByStatus()
    {
        var adminClient = CreateClientForRole("Admin");
        var catId = await SeedCategoryAsync("Report Grouping Category");

        var startDate = new DateTime(2026, 8, 1);
        var endDate = new DateTime(2026, 8, 5);

        // Author 2: 2 bài (1 active, 1 inactive)
        await SeedArticleAsync("RPT_GRP_01", "Bài tác giả 2 - 1", catId, true, 2, new DateTime(2026, 8, 2));
        await SeedArticleAsync("RPT_GRP_02", "Bài tác giả 2 - 2", catId, false, 2, new DateTime(2026, 8, 3));

        // Author 3: 1 bài active
        await SeedArticleAsync("RPT_GRP_03", "Bài tác giả 3 - 1", catId, true, 3, new DateTime(2026, 8, 4));

        // 1. Group by Author
        var authorResponse = await adminClient.GetAsync($"api/report?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&groupBy=author");
        Assert.Equal(HttpStatusCode.OK, authorResponse.StatusCode);
        var authorReport = await authorResponse.Content.ReadFromJsonAsync<NewsReportDto>();
        Assert.NotNull(authorReport);
        Assert.Equal("author", authorReport.GroupBy);
        Assert.Equal(2, authorReport.GroupSummaries.Count);

        var groupAuthor2 = authorReport.GroupSummaries.First(g => g.GroupKey == "2");
        Assert.Equal(2, groupAuthor2.TotalCount);
        Assert.Equal(1, groupAuthor2.ActiveCount);
        Assert.Equal(1, groupAuthor2.InactiveCount);

        // 2. Group by Status
        var statusResponse = await adminClient.GetAsync($"api/report?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&groupBy=status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var statusReport = await statusResponse.Content.ReadFromJsonAsync<NewsReportDto>();
        Assert.NotNull(statusReport);
        Assert.Equal("status", statusReport.GroupBy);

        var activeGroup = statusReport.GroupSummaries.First(g => g.GroupKey == "active");
        Assert.Equal(2, activeGroup.TotalCount);
        var inactiveGroup = statusReport.GroupSummaries.First(g => g.GroupKey == "inactive");
        Assert.Equal(1, inactiveGroup.TotalCount);
    }
}
