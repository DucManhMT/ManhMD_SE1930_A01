using FUNewsManagement_AnalyticsAPI.Models;

namespace FUNewsManagement_AnalyticsAPI.Services;

public interface IExcelExportService
{
    byte[] ExportDashboardReport(
        AnalyticsDashboardDto dashboard,
        List<CoreNewsArticleDto> articles,
        Dictionary<string, int> viewStats,
        DashboardFilterQuery filter);
}
