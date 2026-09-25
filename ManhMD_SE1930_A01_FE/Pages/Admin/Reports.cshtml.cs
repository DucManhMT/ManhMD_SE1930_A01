using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ReportsModel : PageModel
{
    private readonly IReportClientService _reportClientService;
    private readonly ILogger<ReportsModel> _logger;

    public ReportsModel(IReportClientService reportClientService, ILogger<ReportsModel> logger)
    {
        _reportClientService = reportClientService ?? throw new ArgumentNullException(nameof(reportClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string GroupBy { get; set; } = "category";

    public NewsReportApiModel? ReportData { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // 1. Khởi tạo khoảng ngày mặc định nếu chưa truyền (30 ngày gần nhất tính đến hôm nay)
        EndDate ??= DateTime.Today;
        StartDate ??= EndDate.Value.AddDays(-30);

        if (string.IsNullOrWhiteSpace(GroupBy))
        {
            GroupBy = "category";
        }

        // 2. FUN-019: AC 1 - Start <= End
        if (StartDate.Value.Date > EndDate.Value.Date)
        {
            ErrorMessage = "Khoảng ngày không hợp lệ: Ngày bắt đầu không được lớn hơn Ngày kết thúc.";
            return;
        }

        // 3. Tải dữ liệu báo cáo từ API
        try
        {
            ReportData = await _reportClientService.GetReportAsync(StartDate.Value, EndDate.Value, GroupBy, cancellationToken);
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogError(ex, "API error fetching report");
            ErrorMessage = "Không thể tải báo cáo từ hệ thống API: " + ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading report");
            ErrorMessage = "Đã xảy ra lỗi không mong muốn khi tải báo cáo thống kê. Vui lòng thử lại sau.";
        }
    }
}
