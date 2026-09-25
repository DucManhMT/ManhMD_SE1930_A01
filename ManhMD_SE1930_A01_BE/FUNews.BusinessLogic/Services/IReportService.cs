using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Services;

public interface IReportService
{
    Task<NewsReportDto> GenerateReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null, CancellationToken cancellationToken = default);
}
