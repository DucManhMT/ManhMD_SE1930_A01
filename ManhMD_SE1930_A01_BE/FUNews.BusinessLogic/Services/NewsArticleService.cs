using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Helpers;
using FUNews.BusinessLogic.Models;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Sequences;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class NewsArticleService : INewsArticleService
{
    private readonly INewsArticleRepository _articleRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ISqlSequenceService _sqlSequenceService;

    public NewsArticleService(
        INewsArticleRepository articleRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        ISqlSequenceService sqlSequenceService)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _sqlSequenceService = sqlSequenceService ?? throw new ArgumentNullException(nameof(sqlSequenceService));
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

    public async Task<NewsArticleDto> CreateAsync(CreateNewsArticleRequestDto request, short createdById, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validation lengths & required fields (AC 3)
        var headline = NewsArticleValidationHelper.ValidateHeadline(request.Headline);
        var title = NewsArticleValidationHelper.ValidateTitle(request.NewsTitle);
        var content = NewsArticleValidationHelper.ValidateContent(request.NewsContent);
        var source = NewsArticleValidationHelper.ValidateSource(request.NewsSource);

        // 2. Validate Category active (AC 4)
        if (request.CategoryId <= 0)
        {
            throw new ValidationException(nameof(request.CategoryId), "Vui lòng chọn chuyên mục cho bài viết.");
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null || category.IsActive != true)
        {
            throw new ValidationException(nameof(request.CategoryId), "Chuyên mục được chọn không tồn tại hoặc đã bị tạm ẩn.");
        }

        // 3. Validate Tags exist & distinct (AC 5, AC 6)
        var distinctTagIds = NewsArticleValidationHelper.ValidateAndDeduplicateTags(request.TagIds);
        if (distinctTagIds.Count > 0)
        {
            var existingTags = await _tagRepository.GetByIdsAsync(distinctTagIds, cancellationToken);
            if (existingTags.Count != distinctTagIds.Count)
            {
                var existingSet = existingTags.Select(t => t.TagID).ToHashSet();
                var missingIds = distinctTagIds.Where(tid => !existingSet.Contains(tid)).ToList();
                throw new ValidationException("TagIds", $"Thẻ tin với mã {string.Join(", ", missingIds)} không tồn tại trong hệ thống.");
            }
        }

        // 4. Generate unique ID <= 20 chars (AC 2)
        var nextId = await _sqlSequenceService.GetNextNewsArticleIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(nextId) || nextId.Length > NewsArticleValidationHelper.MaxIdLength)
        {
            throw new InvalidOperationException($"Mã bài viết tạo tự động '{nextId}' vượt quá độ dài cho phép ({NewsArticleValidationHelper.MaxIdLength} ký tự).");
        }

        // 5. Construct entity with server-assigned actor/date (AC 1)
        var article = new NewsArticle
        {
            NewsArticleID = nextId,
            NewsTitle = title,
            Headline = headline,
            NewsContent = content,
            NewsSource = source,
            CategoryID = request.CategoryId,
            NewsStatus = request.NewsStatus ?? true,
            CreatedByID = createdById,
            CreatedDate = DateTime.Now,
            UpdatedByID = null,
            ModifiedDate = null
        };

        // 6. Save atomic with NewsTags (AC 6)
        await _articleRepository.CreateWithTagsAsync(article, distinctTagIds, cancellationToken);

        // 7. Return created article DTO
        var created = await GetByIdAsync(nextId, activeOnly: null, cancellationToken);
        return created ?? throw new InvalidOperationException($"Không thể tải lại dữ liệu bài viết vừa tạo '{nextId}'.");
    }
}
