using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class DashboardRazorPageTests
{
    private class FakeAnalyticsClientService : IAnalyticsClientService
    {
        public AnalyticsDashboardApiModel DashboardToReturn { get; set; } = new()
        {
            TotalArticles = 15,
            ActiveArticles = 12,
            InactiveArticles = 3,
            ArticlesByCategory = new List<CategoryStatItem>
            {
                new() { CategoryId = 1, CategoryName = "Công nghệ", Count = 10 },
                new() { CategoryId = 2, CategoryName = "Đời sống", Count = 5 }
            },
            ArticlesByPeriod = new List<PeriodStatItem>
            {
                new() { Period = "2026-05", Count = 6 },
                new() { Period = "2026-06", Count = 9 }
            }
        };

        public List<TrendingArticleApiModel> TrendingToReturn { get; set; } = new()
        {
            new() { NewsArticleId = "TR01", NewsTitle = "Bài 1", ViewCount = 200, CategoryName = "Công nghệ" },
            new() { NewsArticleId = "TR02", NewsTitle = "Bài 2", ViewCount = 150, CategoryName = "Đời sống" }
        };

        public byte[] ExcelBytesToReturn { get; set; } = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x11, 0x22 }; // PK zip header

        public Task<AnalyticsDashboardApiModel> GetDashboardAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            short? categoryId = null,
            bool? status = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DashboardToReturn);
        }

        public Task<List<TrendingArticleApiModel>> GetTrendingArticlesAsync(
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TrendingToReturn);
        }

        public Task<List<RecommendedArticleApiModel>> GetRecommendedArticlesAsync(
            string articleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<RecommendedArticleApiModel>());
        }

        public Task<byte[]> ExportExcelAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            short? categoryId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExcelBytesToReturn);
        }
    }

    private class FakeCategoryClientService : ICategoryClientService
    {
        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<CategoryApiModel>
            {
                Value = new List<CategoryApiModel>
                {
                    new() { CategoryId = 1, CategoryName = "Công nghệ", IsActive = true },
                    new() { CategoryId = 2, CategoryName = "Đời sống", IsActive = true }
                }
            });
        }

        public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryApiModel?>(null);
        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task T11_OnGetAsync_LoadsDashboardDataAndTrendingArticles()
    {
        // Arrange
        var fakeAnalytics = new FakeAnalyticsClientService();
        var fakeCategory = new FakeCategoryClientService();
        var page = new DashboardModel(fakeAnalytics, fakeCategory, NullLogger<DashboardModel>.Instance);

        // Act
        await page.OnGetAsync(CancellationToken.None);

        // Assert
        Assert.Null(page.ErrorMessage);
        Assert.NotNull(page.Dashboard);
        Assert.Equal(15, page.Dashboard.TotalArticles);
        Assert.Equal(12, page.Dashboard.ActiveArticles);
        Assert.Equal(3, page.Dashboard.InactiveArticles);
        Assert.Equal(2, page.TrendingArticles.Count);
        Assert.Equal(350, page.TotalViews); // 200 + 150
        Assert.Equal(2, page.Categories.Count);
    }

    [Fact]
    public async Task T11_OnGetAsync_InvalidDateRange_SetsErrorMessage()
    {
        // Arrange
        var fakeAnalytics = new FakeAnalyticsClientService();
        var fakeCategory = new FakeCategoryClientService();
        var page = new DashboardModel(fakeAnalytics, fakeCategory, NullLogger<DashboardModel>.Instance)
        {
            StartDate = new DateTime(2026, 6, 10),
            EndDate = new DateTime(2026, 6, 1)
        };

        // Act
        await page.OnGetAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(page.ErrorMessage);
        Assert.Contains("Ngày bắt đầu không được lớn hơn Ngày kết thúc", page.ErrorMessage);
    }

    [Fact]
    public async Task T11_OnGetChartDataAsync_ReturnsJsonResultWithDashboardAndTrending()
    {
        // Arrange
        var fakeAnalytics = new FakeAnalyticsClientService();
        var fakeCategory = new FakeCategoryClientService();
        var page = new DashboardModel(fakeAnalytics, fakeCategory, NullLogger<DashboardModel>.Instance);

        // Act
        var result = await page.OnGetChartDataAsync(CancellationToken.None);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task T11_OnGetChartDataAsync_InvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var fakeAnalytics = new FakeAnalyticsClientService();
        var fakeCategory = new FakeCategoryClientService();
        var page = new DashboardModel(fakeAnalytics, fakeCategory, NullLogger<DashboardModel>.Instance)
        {
            StartDate = new DateTime(2026, 7, 10),
            EndDate = new DateTime(2026, 7, 1)
        };

        // Act
        var result = await page.OnGetChartDataAsync(CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task T14_OnGetExportExcelAsync_ReturnsFileContentResultWithXlsxFormat()
    {
        // Arrange
        var fakeAnalytics = new FakeAnalyticsClientService();
        var fakeCategory = new FakeCategoryClientService();
        var page = new DashboardModel(fakeAnalytics, fakeCategory, NullLogger<DashboardModel>.Instance);

        // Act
        var result = await page.OnGetExportExcelAsync(CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.EndsWith(".xlsx", fileResult.FileDownloadName);
        Assert.Equal(6, fileResult.FileContents.Length);
    }
}
