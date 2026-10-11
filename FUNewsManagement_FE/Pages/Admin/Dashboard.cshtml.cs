using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly IAnalyticsClientService _analyticsService;
    private readonly ICategoryClientService _categoryService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(
        IAnalyticsClientService analyticsService,
        ICategoryClientService categoryService,
        ILogger<DashboardModel> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public short? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? Status { get; set; }

    public AnalyticsDashboardApiModel? Dashboard { get; set; }
    public List<TrendingArticleApiModel> TrendingArticles { get; set; } = new();
    public List<CategoryApiModel> Categories { get; set; } = new();
    public int TotalViews { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(cancellationToken);

        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            ErrorMessage = "Khoảng ngày không hợp lệ: Ngày bắt đầu không được lớn hơn Ngày kết thúc.";
            Dashboard = new AnalyticsDashboardApiModel();
            return;
        }

        try
        {
            Dashboard = await _analyticsService.GetDashboardAsync(StartDate, EndDate, CategoryId, Status, cancellationToken);
            TrendingArticles = await _analyticsService.GetTrendingArticlesAsync(10, cancellationToken);
            TotalViews = TrendingArticles.Sum(t => t.ViewCount);
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogError(ex, "API error loading dashboard data: {Message}", ex.Message);
            ErrorMessage = "Không thể tải số liệu từ Analytics API: " + ex.Message;
            Dashboard = new AnalyticsDashboardApiModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading dashboard data");
            ErrorMessage = "Đã xảy ra lỗi không mong muốn khi tải bảng điều khiển thống kê.";
            Dashboard = new AnalyticsDashboardApiModel();
        }
    }

    public async Task<IActionResult> OnGetChartDataAsync(CancellationToken cancellationToken)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            return BadRequest(new { success = false, message = "Ngày bắt đầu không được lớn hơn Ngày kết thúc." });
        }

        try
        {
            var dash = await _analyticsService.GetDashboardAsync(StartDate, EndDate, CategoryId, Status, cancellationToken);
            var trending = await _analyticsService.GetTrendingArticlesAsync(10, cancellationToken);
            return new JsonResult(new
            {
                success = true,
                dashboard = dash,
                trending
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching chart data via ajax");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetExportExcelAsync(CancellationToken cancellationToken)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            TempData["ErrorMessage"] = "Khoảng ngày không hợp lệ để xuất báo cáo.";
            return RedirectToPage();
        }

        try
        {
            var fileBytes = await _analyticsService.ExportExcelAsync(StartDate, EndDate, CategoryId, cancellationToken);
            var fileName = $"FUNews_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Excel from dashboard");
            TempData["ErrorMessage"] = "Lỗi khi xuất tệp Excel: " + ex.Message;
            return RedirectToPage(new { StartDate, EndDate, CategoryId, Status });
        }
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _categoryService.GetCategoriesAsync(cancellationToken: cancellationToken);
            Categories = result.Value ?? new List<CategoryApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch categories for dashboard filter dropdown");
            Categories = new List<CategoryApiModel>();
        }
    }
}
