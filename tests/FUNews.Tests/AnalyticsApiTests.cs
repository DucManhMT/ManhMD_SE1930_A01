using ClosedXML.Excel;
using FUNewsManagement_AnalyticsAPI.Controllers;
using FUNewsManagement_AnalyticsAPI.Models;
using FUNewsManagement_AnalyticsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FUNews.Tests;

public class AnalyticsApiTests
{
    private class MockCoreDataClient : ICoreDataClient
    {
        public List<CoreNewsArticleDto> Articles { get; set; } = new();
        public List<CoreCategoryDto> Categories { get; set; } = new();
        public Dictionary<string, int> ViewStats { get; set; } = new();

        public Task<List<CoreNewsArticleDto>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Articles);

        public Task<List<CoreNewsArticleDto>> GetActiveNewsArticlesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Articles.Where(a => a.NewsStatus == true).ToList());

        public Task<CoreNewsArticleDto?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Articles.FirstOrDefault(a => a.NewsArticleId == id));

        public Task<List<CoreCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Categories);

        public Task<Dictionary<string, int>> GetViewStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ViewStats);
    }

    [Fact]
    public async Task T10_GetDashboardStats_CalculatesMetricsAndAppliesFilters()
    {
        // Arrange
        var mockClient = new MockCoreDataClient
        {
            Categories = new List<CoreCategoryDto>
            {
                new() { CategoryId = 1, CategoryName = "Công nghệ" },
                new() { CategoryId = 2, CategoryName = "Giáo dục" }
            },
            Articles = new List<CoreNewsArticleDto>
            {
                new() { NewsArticleId = "A1", NewsTitle = "Tin 1", CategoryId = 1, CategoryName = "Công nghệ", NewsStatus = true, CreatedById = 10, CreatedDate = new DateTime(2026, 5, 1) },
                new() { NewsArticleId = "A2", NewsTitle = "Tin 2", CategoryId = 1, CategoryName = "Công nghệ", NewsStatus = false, CreatedById = 10, CreatedDate = new DateTime(2026, 5, 10) },
                new() { NewsArticleId = "A3", NewsTitle = "Tin 3", CategoryId = 2, CategoryName = "Giáo dục", NewsStatus = true, CreatedById = 20, CreatedDate = new DateTime(2026, 6, 1) },
                new() { NewsArticleId = "A4", NewsTitle = "Tin 4", CategoryId = 2, CategoryName = "Giáo dục", NewsStatus = true, CreatedById = 10, CreatedDate = new DateTime(2026, 7, 1) }
            }
        };

        var excelService = new ExcelExportService();
        var analyticsService = new AnalyticsService(mockClient, excelService);

        // Act 1: No filters
        var statsAll = await analyticsService.GetDashboardStatsAsync(new DashboardFilterQuery());

        // Assert 1
        Assert.Equal(4, statsAll.TotalArticles);
        Assert.Equal(3, statsAll.ActiveArticles);
        Assert.Equal(1, statsAll.InactiveArticles);
        Assert.Equal(2, statsAll.ArticlesByCategory.Count);
        Assert.Equal(2, statsAll.ByCategory.First(c => c.CategoryId == 1).Count);
        Assert.Equal(2, statsAll.ByCategory.First(c => c.CategoryId == 2).Count);
        Assert.Contains(statsAll.ByStatus, s => s.Status == "Active" && s.Count == 3);
        Assert.Contains(statsAll.ByStatus, s => s.Status == "Inactive" && s.Count == 1);

        // Act 2: Filter by category and date
        var statsFiltered = await analyticsService.GetDashboardStatsAsync(new DashboardFilterQuery
        {
            CategoryId = 1,
            StartDate = new DateTime(2026, 5, 5)
        });

        // Assert 2: Only A2 matches
        Assert.Equal(1, statsFiltered.TotalArticles);
        Assert.Equal(0, statsFiltered.ActiveArticles);
        Assert.Equal(1, statsFiltered.InactiveArticles);
    }

    [Fact]
    public async Task T12_GetTrendingArticles_SortsByViewsDescendingAndLimits()
    {
        // Arrange
        var mockClient = new MockCoreDataClient
        {
            Articles = new List<CoreNewsArticleDto>
            {
                new() { NewsArticleId = "T1", NewsTitle = "Bài 1", CreatedDate = DateTime.UtcNow },
                new() { NewsArticleId = "T2", NewsTitle = "Bài 2", CreatedDate = DateTime.UtcNow },
                new() { NewsArticleId = "T3", NewsTitle = "Bài 3", CreatedDate = DateTime.UtcNow },
                new() { NewsArticleId = "T4", NewsTitle = "Bài 4", CreatedDate = DateTime.UtcNow }
            },
            ViewStats = new Dictionary<string, int>
            {
                ["T1"] = 120,
                ["T2"] = 550,
                ["T3"] = 40,
                ["T4"] = 300
            }
        };

        var analyticsService = new AnalyticsService(mockClient, new ExcelExportService());

        // Act: limit 2
        var trending = await analyticsService.GetTrendingArticlesAsync(limit: 2);

        // Assert: top 2 articles ordered by views
        Assert.Equal(2, trending.Count);
        Assert.Equal("T2", trending[0].NewsArticleId);
        Assert.Equal(550, trending[0].ViewCount);
        Assert.Equal("T4", trending[1].NewsArticleId);
        Assert.Equal(300, trending[1].ViewCount);
    }

    [Fact]
    public async Task T13_GetRecommendedArticles_ReturnsUpTo3RelatedActiveArticlesExcludingCurrent()
    {
        // Arrange
        var originArticle = new CoreNewsArticleDto
        {
            NewsArticleId = "ORIGIN",
            NewsTitle = "Tin gốc",
            CategoryId = 1,
            NewsStatus = true,
            Tags = new List<CoreTagDto>
            {
                new() { TagId = 10, TagName = "AI" },
                new() { TagId = 20, TagName = "DotNet" }
            }
        };

        var relatedA = new CoreNewsArticleDto
        {
            NewsArticleId = "REL_A",
            NewsTitle = "Tin A cùng category và tag",
            CategoryId = 1,
            NewsStatus = true,
            CreatedDate = DateTime.UtcNow.AddMinutes(-10),
            Tags = new List<CoreTagDto> { new() { TagId = 10, TagName = "AI" } }
        };

        var relatedB = new CoreNewsArticleDto
        {
            NewsArticleId = "REL_B",
            NewsTitle = "Tin B chỉ cùng tag",
            CategoryId = 2,
            NewsStatus = true,
            CreatedDate = DateTime.UtcNow.AddMinutes(-20),
            Tags = new List<CoreTagDto> { new() { TagId = 20, TagName = "DotNet" } }
        };

        var relatedC = new CoreNewsArticleDto
        {
            NewsArticleId = "REL_C",
            NewsTitle = "Tin C chỉ cùng category",
            CategoryId = 1,
            NewsStatus = true,
            CreatedDate = DateTime.UtcNow.AddMinutes(-30)
        };

        var inactiveArticle = new CoreNewsArticleDto
        {
            NewsArticleId = "INACTIVE",
            NewsTitle = "Tin Inactive cùng category và tag",
            CategoryId = 1,
            NewsStatus = false,
            Tags = new List<CoreTagDto> { new() { TagId = 10, TagName = "AI" } }
        };

        var unrelatedArticle = new CoreNewsArticleDto
        {
            NewsArticleId = "UNRELATED",
            NewsTitle = "Tin không liên quan",
            CategoryId = 99,
            NewsStatus = true,
            Tags = new List<CoreTagDto> { new() { TagId = 99, TagName = "Other" } }
        };

        var mockClient = new MockCoreDataClient
        {
            Articles = new List<CoreNewsArticleDto>
            {
                originArticle,
                relatedA,
                relatedB,
                relatedC,
                inactiveArticle,
                unrelatedArticle
            }
        };

        var analyticsService = new AnalyticsService(mockClient, new ExcelExportService());

        // Act
        var result = await analyticsService.GetRecommendedArticlesAsync("ORIGIN");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, r => r.NewsArticleId == "ORIGIN");
        Assert.DoesNotContain(result, r => r.NewsArticleId == "INACTIVE");
        Assert.DoesNotContain(result, r => r.NewsArticleId == "UNRELATED");
        Assert.Equal("REL_A", result[0].NewsArticleId); // Highest score (same category + common tag)
    }

    [Fact]
    public async Task T13_GetRecommendedArticles_ReturnsNullWhenTargetNotFound()
    {
        // Arrange
        var mockClient = new MockCoreDataClient { Articles = new List<CoreNewsArticleDto>() };
        var analyticsService = new AnalyticsService(mockClient, new ExcelExportService());

        // Act
        var result = await analyticsService.GetRecommendedArticlesAsync("NON_EXISTING");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task T14_ExportExcelReport_GeneratesValidClosedXmlWorkbook()
    {
        // Arrange
        var mockClient = new MockCoreDataClient
        {
            Categories = new List<CoreCategoryDto>
            {
                new() { CategoryId = 1, CategoryName = "Công nghệ" }
            },
            Articles = new List<CoreNewsArticleDto>
            {
                new() { NewsArticleId = "E1", NewsTitle = "Bài báo xuất Excel", CategoryId = 1, CategoryName = "Công nghệ", NewsStatus = true, CreatedDate = DateTime.UtcNow }
            },
            ViewStats = new Dictionary<string, int> { ["E1"] = 42 }
        };

        var analyticsService = new AnalyticsService(mockClient, new ExcelExportService());

        // Act
        var bytes = await analyticsService.ExportExcelReportAsync(new DashboardFilterQuery());

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);

        // Verify valid excel format with ClosedXML
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.NotNull(workbook.Worksheet("Thống Kê Tổng Quan"));
        Assert.NotNull(workbook.Worksheet("Chi Tiết Bài Viết"));
    }

    [Fact]
    public async Task AnalyticsController_EndpointsReturnExpectedResponses()
    {
        // Arrange
        var mockClient = new MockCoreDataClient
        {
            Articles = new List<CoreNewsArticleDto>
            {
                new() { NewsArticleId = "C1", NewsTitle = "Test", NewsStatus = true, CategoryId = 1 }
            }
        };
        var analyticsService = new AnalyticsService(mockClient, new ExcelExportService());
        var controller = new AnalyticsController(analyticsService);

        // Act & Assert Dashboard
        var dashRes = await controller.GetDashboard(new DashboardFilterQuery(), CancellationToken.None);
        var okDash = Assert.IsType<OkObjectResult>(dashRes.Result);
        var dashModel = Assert.IsType<AnalyticsDashboardDto>(okDash.Value);
        Assert.Equal(1, dashModel.TotalArticles);

        // Act & Assert Trending
        var trendRes = await controller.GetTrending(5, CancellationToken.None);
        var okTrend = Assert.IsType<OkObjectResult>(trendRes.Result);
        var trendList = Assert.IsType<List<TrendingArticleDto>>(okTrend.Value);
        Assert.Single(trendList);

        // Act & Assert Export
        var exportRes = await controller.ExportExcel(new DashboardFilterQuery(), CancellationToken.None);
        var fileResult = Assert.IsType<FileContentResult>(exportRes);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task RecommendController_Returns404WhenArticleNotFound()
    {
        // Arrange
        var mockClient = new MockCoreDataClient { Articles = new List<CoreNewsArticleDto>() };
        var analyticsService = new AnalyticsService(mockClient, new ExcelExportService());
        var controller = new RecommendController(analyticsService);

        // Act
        var result = await controller.GetRecommendations("UNKNOWN_ID", CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, problem.Status);
    }
}
