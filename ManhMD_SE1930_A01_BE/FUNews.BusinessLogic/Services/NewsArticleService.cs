using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Helpers;
using FUNews.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class NewsArticleService : INewsArticleService
{
    private readonly INewsArticleRepository _articleRepository;

    public NewsArticleService(INewsArticleRepository articleRepository)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
    }

    public IQueryable<NewsArticleDto> GetQueryable(bool? activeOnly = null)
    {
        var query = _articleRepository.GetQueryable().AsNoTracking();
        if (activeOnly == true)
        {
            query = query.Where(a => a.NewsStatus == true);
        }

        return query
            .OrderByDescending(a => a.CreatedDate)
            .ThenByDescending(a => a.NewsArticleID)
            .Select(NewsArticleMappingHelper.ProjectToDto);
    }

    public async Task<NewsArticleDto?> GetByIdAsync(string id, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _articleRepository.GetQueryable().AsNoTracking().Where(a => a.NewsArticleID == id);
        if (activeOnly == true)
        {
            query = query.Where(a => a.NewsStatus == true);
        }

        return await query
            .Select(NewsArticleMappingHelper.ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
