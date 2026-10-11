namespace FUNews.DataAccess.Sequences;

public interface ISqlSequenceService
{
    Task<short> GetNextAccountIdAsync(CancellationToken cancellationToken = default);
    Task<int> GetNextTagIdAsync(CancellationToken cancellationToken = default);
    Task<string> GetNextNewsArticleIdAsync(CancellationToken cancellationToken = default);
}
