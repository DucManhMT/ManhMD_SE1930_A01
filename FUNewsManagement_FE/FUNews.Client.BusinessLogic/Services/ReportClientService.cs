using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class ReportClientService : IReportClientService
{
    private readonly IFUNewsApiClient _apiClient;

    public ReportClientService(IFUNewsApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<NewsReportApiModel> GetReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetReportAsync(startDate, endDate, groupBy, cancellationToken);
    }
}
