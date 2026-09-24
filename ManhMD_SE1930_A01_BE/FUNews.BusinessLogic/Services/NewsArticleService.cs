using FUNews.BusinessLogic.DTOs;
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
            .Select(a => new NewsArticleDto
            {
                NewsArticleId = a.NewsArticleID,
                NewsTitle = a.NewsTitle,
                Headline = a.Headline,
                NewsContent = a.NewsContent,
                NewsSource = a.NewsSource,
                CategoryId = a.CategoryID,
                CategoryName = a.Category != null ? a.Category.CategoryName : null,
                NewsStatus = a.NewsStatus,
                CreatedById = a.CreatedByID,
                AuthorName = a.CreatedBy != null ? a.CreatedBy.AccountName : null,
                CreatedDate = a.CreatedDate,
                UpdatedById = a.UpdatedByID,
                LastEditorName = a.UpdatedBy != null ? a.UpdatedBy.AccountName : null,
                ModifiedDate = a.ModifiedDate,
                Tags = a.NewsTags.Select(nt => new TagDto
                {
                    TagId = nt.TagID,
                    TagName = nt.Tag != null ? nt.Tag.TagName : null,
                    Note = nt.Tag != null ? nt.Tag.Note : null
                }).ToList()
            });
    }

    public async Task<NewsArticleDto?> GetByIdAsync(string id, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _articleRepository.GetQueryable().AsNoTracking().Where(a => a.NewsArticleID == id);
        if (activeOnly == true)
        {
            query = query.Where(a => a.NewsStatus == true);
        }

        return await query.Select(a => new NewsArticleDto
        {
            NewsArticleId = a.NewsArticleID,
            NewsTitle = a.NewsTitle,
            Headline = a.Headline,
            NewsContent = a.NewsContent,
            NewsSource = a.NewsSource,
            CategoryId = a.CategoryID,
            CategoryName = a.Category != null ? a.Category.CategoryName : null,
            NewsStatus = a.NewsStatus,
            CreatedById = a.CreatedByID,
            AuthorName = a.CreatedBy != null ? a.CreatedBy.AccountName : null,
            CreatedDate = a.CreatedDate,
            UpdatedById = a.UpdatedByID,
            LastEditorName = a.UpdatedBy != null ? a.UpdatedBy.AccountName : null,
            ModifiedDate = a.ModifiedDate,
            Tags = a.NewsTags.Select(nt => new TagDto
            {
                TagId = nt.TagID,
                TagName = nt.Tag != null ? nt.Tag.TagName : null,
                Note = nt.Tag != null ? nt.Tag.Note : null
            }).ToList()
        }).FirstOrDefaultAsync(cancellationToken);
    }
}
