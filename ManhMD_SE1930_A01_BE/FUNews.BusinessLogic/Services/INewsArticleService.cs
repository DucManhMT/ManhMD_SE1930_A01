using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Services;

public interface INewsArticleService
{
    IQueryable<NewsArticleDto> GetQueryable(bool? activeOnly = null);
    Task<NewsArticleDto?> GetByIdAsync(string id, bool? activeOnly = null, CancellationToken cancellationToken = default);
}
