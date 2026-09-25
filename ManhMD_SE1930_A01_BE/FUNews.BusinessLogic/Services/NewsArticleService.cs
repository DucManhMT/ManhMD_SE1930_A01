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

    public async Task<NewsArticleDto> UpdateAsync(string id, UpdateNewsArticleRequestDto request, short updatedById, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException("id", "Mã bài viết không hợp lệ.");
        }

        // 1. Kiểm tra bài viết tồn tại
        var existing = await _articleRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new NotFoundException($"Không tìm thấy bài viết với mã '{id}'.");
        }

        // 2. Validation lengths & required fields
        var headline = NewsArticleValidationHelper.ValidateHeadline(request.Headline);
        var title = NewsArticleValidationHelper.ValidateTitle(request.NewsTitle);
        var content = NewsArticleValidationHelper.ValidateContent(request.NewsContent);
        var source = NewsArticleValidationHelper.ValidateSource(request.NewsSource);

        // 3. Validate Category:
        //    D08: Được giữ category inactive cũ (AC 3) — chỉ từ chối nếu category mới không tồn tại.
        //    Nếu đổi sang category mới, category mới phải active.
        //    Nếu giữ nguyên category cũ dù đang inactive, cho phép.
        if (request.CategoryId <= 0)
        {
            throw new ValidationException(nameof(request.CategoryId), "Vui lòng chọn chuyên mục cho bài viết.");
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new ValidationException(nameof(request.CategoryId), "Chuyên mục được chọn không tồn tại.");
        }

        // Nếu đổi sang category khác, category đó phải active (D08)
        if (request.CategoryId != existing.CategoryID && category.IsActive != true)
        {
            throw new ValidationException(nameof(request.CategoryId), "Không thể chuyển bài viết sang chuyên mục đã bị tạm ẩn.");
        }

        // 4. Validate Tags exist & distinct
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

        // 5. Cập nhật các trường cho phép, giữ nguyên CreatedByID và CreatedDate (AC 1)
        existing.NewsTitle = title;
        existing.Headline = headline;
        existing.NewsContent = content;
        existing.NewsSource = source;
        existing.CategoryID = request.CategoryId;
        existing.NewsStatus = request.NewsStatus ?? existing.NewsStatus;
        // AC 2: Server gán UpdatedByID và ModifiedDate
        existing.UpdatedByID = updatedById;
        existing.ModifiedDate = DateTime.Now;
        // CreatedByID và CreatedDate giữ nguyên như trong entity đã đọc từ DB

        // 6. Lưu bài viết và thay thế toàn bộ tags (AC 4: atomic)
        await _articleRepository.UpdateWithTagsAsync(existing, distinctTagIds, cancellationToken);

        // 7. Đọc lại và trả về DTO đầy đủ
        var updated = await GetByIdAsync(id, activeOnly: null, cancellationToken);
        return updated ?? throw new InvalidOperationException($"Không thể tải lại dữ liệu bài viết vừa cập nhật '{id}'.");
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException("id", "Mã bài viết không hợp lệ.");
        }

        // Kiểm tra bài tồn tại trước khi xóa (trả về lỗi rõ ràng hơn false)
        var exists = await _articleRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Không tìm thấy bài viết với mã '{id}'.");
        }

        // AC 5: Repository sẽ xóa NewsTags trước rồi mới xóa bài (trong transaction)
        // AC 6: Chỉ xóa NewsTags của bài này, bài khác cùng tag không bị ảnh hưởng
        return await _articleRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<NewsArticleDto> DuplicateAsync(string sourceId, short currentStaffId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ValidationException("id", "Mã bài viết nguồn không hợp lệ.");
        }

        // 1. Kiểm tra bài viết nguồn tồn tại kèm danh sách tags (AC 7: bài gốc không đổi)
        var source = await _articleRepository.GetByIdWithDetailsAsync(sourceId, cancellationToken);
        if (source == null)
        {
            throw new NotFoundException($"Không tìm thấy bài viết nguồn với mã '{sourceId}'.");
        }

        // 2. Sinh ID mới duy nhất qua SQL sequence (AC 1)
        var nextId = await _sqlSequenceService.GetNextNewsArticleIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(nextId) || nextId.Length > NewsArticleValidationHelper.MaxIdLength)
        {
            throw new InvalidOperationException($"Mã bài viết nhân bản tự động '{nextId}' vượt quá độ dài cho phép ({NewsArticleValidationHelper.MaxIdLength} ký tự).");
        }

        // 3. Sao chép nội dung/category/tags; trạng thái Inactive; tác giả hiện tại; ngày mới; UpdatedBy/ModifiedDate NULL (AC 2, AC 3, AC 4, AC 7)
        var duplicateArticle = new NewsArticle
        {
            NewsArticleID = nextId,
            NewsTitle = source.NewsTitle,
            Headline = source.Headline,
            NewsContent = source.NewsContent,
            NewsSource = source.NewsSource,
            CategoryID = source.CategoryID,
            NewsStatus = false, // AC 2: Luôn là Inactive (bản sao chưa publish)
            CreatedByID = currentStaffId, // AC 3: Tác giả là Staff đang thực hiện thao tác
            CreatedDate = DateTime.Now, // AC 3: Thời điểm nhân bản
            UpdatedByID = null, // AC 4: Audit update khởi tạo NULL
            ModifiedDate = null // AC 4: Audit update khởi tạo NULL
        };

        var tagIds = source.NewsTags?.Select(nt => nt.TagID).Distinct().ToList() ?? new List<int>();

        // 4. Lưu atomic bài viết mới và các tags phụ thuộc trong transaction (AC 5, AC 6)
        await _articleRepository.CreateWithTagsAsync(duplicateArticle, tagIds, cancellationToken);

        // 5. Trả về DTO của bản sao vừa tạo
        var createdDto = await GetByIdAsync(nextId, activeOnly: null, cancellationToken);
        return createdDto ?? throw new InvalidOperationException($"Không thể tải lại dữ liệu bài viết vừa nhân bản '{nextId}'.");
    }
}

