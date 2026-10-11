using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<NewsReportDto>> GetReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken)
    {
        var end = endDate ?? DateTime.Today;
        var start = startDate ?? end.AddDays(-30);

        // FUN-019: AC 1 - Start <= End
        if (start.Date > end.Date)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Khoảng ngày không hợp lệ",
                Detail = "Khoảng ngày không hợp lệ: Ngày bắt đầu không được lớn hơn ngày kết thúc."
            });
        }

        try
        {
            var report = await _reportService.GenerateReportAsync(start, end, groupBy, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu yêu cầu không hợp lệ",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating report from {Start} to {End}", start, end);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Lỗi hệ thống",
                Detail = "Đã xảy ra lỗi khi tạo báo cáo thống kê."
            });
        }
    }
}
