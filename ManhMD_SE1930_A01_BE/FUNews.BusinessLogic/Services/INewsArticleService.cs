using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;

namespace FUNews.BusinessLogic.Services;

public interface INewsArticleService
{
    IQueryable<NewsArticleDto> GetQueryable(bool? activeOnly = null);
    Task<NewsArticleDto?> GetByIdAsync(string id, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<NewsArticleDto> CreateAsync(CreateNewsArticleRequestDto request, short createdById, CancellationToken cancellationToken = default);
}
