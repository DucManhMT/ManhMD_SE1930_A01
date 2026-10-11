using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Helpers;
using FUNews.BusinessLogic.Models;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Sequences;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly ISqlSequenceService _sqlSequenceService;

    public TagService(ITagRepository tagRepository, ISqlSequenceService sqlSequenceService)
    {
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _sqlSequenceService = sqlSequenceService ?? throw new ArgumentNullException(nameof(sqlSequenceService));
    }

    public IQueryable<TagDto> GetQueryable(bool isStaff = false)
    {
        return _tagRepository.GetQueryable()
            .AsNoTracking()
            .Select(TagMappingHelper.GetProjectToDto(isStaff));
    }

    public async Task<TagDto?> GetByIdAsync(int id, bool isStaff = false, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdWithNewsTagsAsync(id, asNoTracking: true, cancellationToken);
        return tag != null ? TagMappingHelper.ToDto(tag, isStaff) : null;
    }

    public async Task<TagDto> CreateAsync(CreateTagRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedName = TagValidationHelper.ValidateAndTrimName(request.TagName);
        var trimmedNote = TagValidationHelper.ValidateAndTrimNote(request.Note);

        var isUnique = await _tagRepository.IsNameUniqueAsync(trimmedName, null, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.TagName), TagValidationHelper.DuplicateNameMessage);
        }

        var nextId = await _sqlSequenceService.GetNextTagIdAsync(cancellationToken);
        while (await _tagRepository.ExistsAsync(nextId, cancellationToken))
        {
            nextId++;
        }

        var tag = new Tag
        {
            TagID = nextId,
            TagName = trimmedName,
            Note = trimmedNote
        };

        try
        {
            var created = await _tagRepository.AddAsync(tag, cancellationToken);
            return TagMappingHelper.ToDto(created, isStaff: true);
        }
        catch (DbUpdateException ex)
        {
            TagValidationHelper.HandleDbUpdateException(ex);
            throw;
        }
    }

    public async Task<TagDto> UpdateAsync(int id, UpdateTagRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _tagRepository.GetByIdWithNewsTagsAsync(id, asNoTracking: false, cancellationToken);
        if (existing == null)
        {
            throw new NotFoundException($"Thẻ tin với mã {id} không tồn tại.");
        }

        var trimmedName = TagValidationHelper.ValidateAndTrimName(request.TagName);
        var trimmedNote = TagValidationHelper.ValidateAndTrimNote(request.Note);

        var isUnique = await _tagRepository.IsNameUniqueAsync(trimmedName, id, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.TagName), TagValidationHelper.DuplicateNameMessage);
        }

        existing.TagName = trimmedName;
        existing.Note = trimmedNote;

        try
        {
            var updated = await _tagRepository.UpdateAsync(existing, cancellationToken);
            return TagMappingHelper.ToDto(updated, isStaff: true);
        }
        catch (DbUpdateException ex)
        {
            TagValidationHelper.HandleDbUpdateException(ex);
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new NotFoundException($"Thẻ tin với mã {id} không tồn tại.");
        }

        // AC 2 & AC 5: Chặn xóa nếu đang gắn với bài viết
        if (await _tagRepository.HasArticlesAsync(id, cancellationToken))
        {
            throw new ConflictException("Không thể xóa thẻ tin vì đang được gắn với bài viết.");
        }

        try
        {
            await _tagRepository.DeleteAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message, ex);
        }
    }

    public async Task<List<NewsArticleDto>> GetArticlesByTagAsync(int tagId, bool isStaff = false, CancellationToken cancellationToken = default)
    {
        var exists = await _tagRepository.ExistsAsync(tagId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Thẻ tin với mã {tagId} không tồn tại.");
        }

        // AC 4: Public/Anonymous chỉ thấy Active; Staff thấy tất cả
        bool? activeOnly = isStaff ? null : true;
        var articles = await _tagRepository.GetArticlesByTagAsync(tagId, activeOnly, cancellationToken);

        return articles.Select(NewsArticleMappingHelper.ToDto).ToList();
    }
}
