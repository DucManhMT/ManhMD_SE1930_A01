using FUNewsManagement_AnalyticsAPI.Models;
using FUNewsManagement_AnalyticsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsManagement_AnalyticsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetDashboard(
        [FromQuery] DashboardFilterQuery filter,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetDashboardStatsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("trending")]
    public async Task<ActionResult<List<TrendingArticleDto>>> GetTrending(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetTrendingArticlesAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] DashboardFilterQuery filter,
        CancellationToken cancellationToken)
    {
        var fileBytes = await _analyticsService.ExportExcelReportAsync(filter, cancellationToken);
        var fileName = $"FUNews_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
