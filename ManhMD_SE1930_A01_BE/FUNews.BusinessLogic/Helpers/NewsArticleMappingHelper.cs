using System.Linq.Expressions;
using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class NewsArticleMappingHelper
{
    public static readonly Expression<Func<NewsArticle, NewsArticleDto>> ProjectToDto = a => new NewsArticleDto
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
    };

    public static NewsArticleDto ToDto(NewsArticle a)
    {
        ArgumentNullException.ThrowIfNull(a);

        return new NewsArticleDto
        {
            NewsArticleId = a.NewsArticleID,
            NewsTitle = a.NewsTitle,
            Headline = a.Headline,
            NewsContent = a.NewsContent,
            NewsSource = a.NewsSource,
            CategoryId = a.CategoryID,
            CategoryName = a.Category?.CategoryName,
            NewsStatus = a.NewsStatus,
            CreatedById = a.CreatedByID,
            AuthorName = a.CreatedBy?.AccountName,
            CreatedDate = a.CreatedDate,
            UpdatedById = a.UpdatedByID,
            LastEditorName = a.UpdatedBy?.AccountName,
            ModifiedDate = a.ModifiedDate,
            Tags = a.NewsTags?.Select(nt => new TagDto
            {
                TagId = nt.TagID,
                TagName = nt.Tag?.TagName,
                Note = nt.Tag?.Note
            }).ToList() ?? new List<TagDto>()
        };
    }
}
