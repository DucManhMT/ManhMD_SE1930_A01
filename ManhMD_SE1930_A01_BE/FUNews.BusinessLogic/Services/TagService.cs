using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
    }

    public IQueryable<TagDto> GetQueryable()
    {
        return _tagRepository.GetQueryable()
            .AsNoTracking()
            .Select(t => new TagDto
            {
                TagId = t.TagID,
                TagName = t.TagName,
                Note = t.Note
            });
    }

    public async Task<TagDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag == null) return null;

        return new TagDto
        {
            TagId = tag.TagID,
            TagName = tag.TagName,
            Note = tag.Note
        };
    }
}
