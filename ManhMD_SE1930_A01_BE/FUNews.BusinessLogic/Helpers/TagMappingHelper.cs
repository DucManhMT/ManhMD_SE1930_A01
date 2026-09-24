using System.Linq.Expressions;
using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class TagMappingHelper
{
    public static readonly Expression<Func<Tag, TagDto>> ProjectToDto = t => new TagDto
    {
        TagId = t.TagID,
        TagName = t.TagName,
        Note = t.Note
    };

    public static TagDto ToDto(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new TagDto
        {
            TagId = tag.TagID,
            TagName = tag.TagName,
            Note = tag.Note
        };
    }
}
