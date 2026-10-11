namespace FUNews.DataAccess.Repositories;

public interface INewsViewRepository
{
    Task RecordViewAsync(string newsArticleId, string? visitorIp = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetViewStatsAsync(CancellationToken cancellationToken = default);
    Task<int> GetViewCountAsync(string newsArticleId, CancellationToken cancellationToken = default);
}
