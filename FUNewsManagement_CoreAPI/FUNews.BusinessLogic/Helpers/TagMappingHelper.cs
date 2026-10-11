using System.Linq.Expressions;
using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class TagMappingHelper
{
    public static readonly Expression<Func<Tag, TagDto>> ProjectToDto = GetProjectToDto(false);

    public static Expression<Func<Tag, TagDto>> GetProjectToDto(bool isStaff = false)
    {
        if (isStaff)
        {
            return t => new TagDto
            {
                TagId = t.TagID,
                TagName = t.TagName,
                Note = t.Note,
                ArticleCount = t.NewsTags.Count()
            };
        }

        return t => new TagDto
        {
            TagId = t.TagID,
            TagName = t.TagName,
            Note = t.Note,
            ArticleCount = t.NewsTags.Count(nt => nt.NewsArticle != null && nt.NewsArticle.NewsStatus == true)
        };
    }

    public static TagDto ToDto(Tag tag, bool isStaff = false)
    {
        ArgumentNullException.ThrowIfNull(tag);

        int count = 0;
        if (tag.NewsTags != null)
        {
            count = isStaff
                ? tag.NewsTags.Count
                : tag.NewsTags.Count(nt => nt.NewsArticle != null && nt.NewsArticle.NewsStatus == true);
        }

        return new TagDto
        {
            TagId = tag.TagID,
            TagName = tag.TagName,
            Note = tag.Note,
            ArticleCount = count
        };
    }
}
