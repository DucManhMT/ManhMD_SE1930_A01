using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Helpers;
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
            .Select(TagMappingHelper.ProjectToDto);
    }

    public async Task<TagDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        return tag != null ? TagMappingHelper.ToDto(tag) : null;
    }
}
