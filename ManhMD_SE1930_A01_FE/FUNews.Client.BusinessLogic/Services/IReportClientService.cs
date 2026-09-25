using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IReportClientService
{
    Task<NewsReportApiModel> GetReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null, CancellationToken cancellationToken = default);
}
