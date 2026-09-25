using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Pages.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class ReportsRazorPageTests
{
    private class FakeReportClientService : IReportClientService
    {
        public int CallCount { get; private set; }
        public DateTime? LastStartDate { get; private set; }
        public DateTime? LastEndDate { get; private set; }
        public string? LastGroupBy { get; private set; }
        public bool ThrowApiException { get; set; }

        public NewsReportApiModel ResponseToReturn { get; set; } = new()
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            GroupBy = "category",
            TotalArticles = 10,
            TotalActive = 8,
            TotalInactive = 2,
            GroupSummaries = new List<ReportGroupSummaryApiModel>
            {
                new()
                {
                    GroupKey = "1",
                    GroupName = "Thời sự",
                    TotalCount = 6,
                    ActiveCount = 5,
                    InactiveCount = 1
                }
            },
            Details = new List<ReportDetailApiModel>
            {
                new()
                {
                    NewsArticleId = "TEST_01",
                    NewsTitle = "Tin nóng",
                    Headline = "Tóm tắt tin nóng",
                    CategoryName = "Thời sự",
                    AuthorName = "Nhà báo A",
                    CreatedDate = DateTime.Today,
                    NewsStatus = true,
                    LastEditorName = "Chưa chỉnh sửa",
                    ModifiedDate = null
                }
            }
        };

        public Task<NewsReportApiModel> GetReportAsync(
            DateTime startDate,
            DateTime endDate,
            string? groupBy = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastStartDate = startDate;
            LastEndDate = endDate;
            LastGroupBy = groupBy;

            if (ThrowApiException)
            {
                throw new FUNewsApiException(System.Net.HttpStatusCode.InternalServerError, "Lỗi kết nối máy chủ API");
            }

            return Task.FromResult(ResponseToReturn);
        }
    }

    [Fact]
    public async Task OnGetAsync_DefaultDates_Sets30DaysRange_AndCallsService()
    {
        var fakeService = new FakeReportClientService();
        var model = new ReportsModel(fakeService, NullLogger<ReportsModel>.Instance);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(1, fakeService.CallCount);
        Assert.NotNull(model.StartDate);
        Assert.NotNull(model.EndDate);
        Assert.Equal(DateTime.Today, model.EndDate.Value.Date);
        Assert.Equal(DateTime.Today.AddDays(-30), model.StartDate.Value.Date);
        Assert.Equal("category", model.GroupBy);
        Assert.NotNull(model.ReportData);
        Assert.Equal(10, model.ReportData.TotalArticles);
        Assert.Null(model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_StartDateGreaterThanEndDate_SetsErrorMessage_AndDoesNotCallService()
    {
        var fakeService = new FakeReportClientService();
        var model = new ReportsModel(fakeService, NullLogger<ReportsModel>.Instance)
        {
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(0, fakeService.CallCount);
        Assert.Null(model.ReportData);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Khoảng ngày không hợp lệ", model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_PassesCustomGroupByAndDates_ToService()
    {
        var fakeService = new FakeReportClientService();
        var model = new ReportsModel(fakeService, NullLogger<ReportsModel>.Instance)
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            GroupBy = "author"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(1, fakeService.CallCount);
        Assert.Equal(new DateTime(2026, 1, 1), fakeService.LastStartDate);
        Assert.Equal(new DateTime(2026, 1, 31), fakeService.LastEndDate);
        Assert.Equal("author", fakeService.LastGroupBy);
        Assert.NotNull(model.ReportData);
        Assert.Null(model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_ApiThrowsException_SetsErrorMessageGracefully()
    {
        var fakeService = new FakeReportClientService
        {
            ThrowApiException = true
        };
        var model = new ReportsModel(fakeService, NullLogger<ReportsModel>.Instance);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(1, fakeService.CallCount);
        Assert.Null(model.ReportData);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Không thể tải báo cáo từ hệ thống API", model.ErrorMessage);
    }
}
