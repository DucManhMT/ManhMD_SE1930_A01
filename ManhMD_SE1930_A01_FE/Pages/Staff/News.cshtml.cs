using System.ComponentModel.DataAnnotations;
using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Staff;

[Authorize(Roles = "Staff")]
public class NewsModel : PageModel
{
    private readonly INewsClientService _newsClientService;
    private readonly ICategoryClientService _categoryClientService;
    private readonly ITagClientService _tagClientService;
    private readonly ILogger<NewsModel> _logger;

    public NewsModel(
        INewsClientService newsClientService,
        ICategoryClientService categoryClientService,
        ITagClientService tagClientService,
        ILogger<NewsModel> logger)
    {
        _newsClientService = newsClientService ?? throw new ArgumentNullException(nameof(newsClientService));
        _categoryClientService = categoryClientService ?? throw new ArgumentNullException(nameof(categoryClientService));
        _tagClientService = tagClientService ?? throw new ArgumentNullException(nameof(tagClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<NewsArticleApiModel> NewsArticles { get; set; } = new();
    public List<CategoryApiModel> Categories { get; set; } = new();
    public List<TagApiModel> Tags { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public short? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AuthorTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "date_desc";

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // 1. Tải danh mục phục vụ dropdown bộ lọc và modal tạo bài viết
        try
        {
            var catEnvelope = await _categoryClientService.GetCategoriesAsync("$orderby=categoryName asc&$top=100", cancellationToken);
            Categories = catEnvelope?.Value ?? new List<CategoryApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load categories for news filter dropdown.");
            Categories = new List<CategoryApiModel>();
        }

        // 2. Tải thẻ tin phục vụ modal tạo và sửa bài viết
        try
        {
            var tagEnvelope = await _tagClientService.GetTagsAsync("$orderby=tagName asc&$top=100", cancellationToken);
            Tags = tagEnvelope?.Value ?? new List<TagApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load tags for news create/edit modal.");
            Tags = new List<TagApiModel>();
        }

        // 3. Validate ngày hợp lệ (AC 3)
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            ErrorMessage = "Khoảng ngày không hợp lệ: 'Từ ngày' không được lớn hơn 'Đến ngày'.";
            NewsArticles = new List<NewsArticleApiModel>();
            TotalCount = 0;
            return;
        }

        if (PageIndex < 1) PageIndex = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 100) PageSize = 100;

        // 4. Xây dựng OData query và lấy dữ liệu bài viết
        try
        {
            int skip = (PageIndex - 1) * PageSize;
            var odataQuery = ODataFilterHelper.BuildNewsQuery(
                searchTerm: SearchTerm,
                categoryId: CategoryId,
                statusFilter: StatusFilter,
                authorTerm: AuthorTerm,
                startDate: StartDate,
                endDate: EndDate,
                sortBy: SortBy,
                top: PageSize,
                skip: skip
            );

            var envelope = await _newsClientService.GetNewsArticlesAsync(odataQuery, cancellationToken);
            NewsArticles = envelope?.Value ?? new List<NewsArticleApiModel>();
            TotalCount = (int)(envelope?.Count ?? NewsArticles.Count);
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error fetching news articles: {Message}", ex.Message);
            ErrorMessage = ex.Message ?? "Không thể kết nối đến máy chủ lấy danh sách bài viết.";
            NewsArticles = new List<NewsArticleApiModel>();
            TotalCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching news articles.");
            ErrorMessage = "Đã xảy ra lỗi không mong muốn khi tải danh sách bài viết.";
            NewsArticles = new List<NewsArticleApiModel>();
            TotalCount = 0;
        }
    }

    public async Task<IActionResult> OnGetDetailAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new JsonResult(new { success = false, message = "Mã bài viết không hợp lệ." });
        }

        try
        {
            var article = await _newsClientService.GetByIdAsync(id, cancellationToken);
            if (article == null)
            {
                return new JsonResult(new { success = false, message = $"Không tìm thấy bài viết mã '{id}'." });
            }

            return new JsonResult(new { success = true, article });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error fetching detail for news article {Id}: {Message}", id, ex.Message);
            return new JsonResult(new { success = false, message = ex.Message ?? "Không thể lấy thông tin chi tiết bài viết." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching detail for news article {Id}", id);
            return new JsonResult(new { success = false, message = "Lỗi máy chủ khi lấy chi tiết bài viết." });
        }
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateNewsArticleInputModel input, CancellationToken cancellationToken)
    {
        if (input == null)
        {
            return new JsonResult(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin nhập liệu.",
                errors = ValidationResponseHelper.ExtractModelStateErrors(ModelState)
            });
        }

        try
        {
            var request = new CreateNewsArticleApiModel
            {
                NewsTitle = string.IsNullOrWhiteSpace(input.NewsTitle) ? null : input.NewsTitle.Trim(),
                Headline = input.Headline.Trim(),
                NewsContent = string.IsNullOrWhiteSpace(input.NewsContent) ? null : input.NewsContent.Trim(),
                NewsSource = string.IsNullOrWhiteSpace(input.NewsSource) ? null : input.NewsSource.Trim(),
                CategoryId = input.CategoryId,
                NewsStatus = input.NewsStatus,
                TagIds = input.TagIds ?? new List<int>()
            };

            var createdArticle = await _newsClientService.CreateNewsArticleAsync(request, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = $"Tạo bài viết \"{(string.IsNullOrWhiteSpace(createdArticle.NewsTitle) ? createdArticle.Headline : createdArticle.NewsTitle)}\" thành công.",
                article = createdArticle
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while creating news article: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể tạo bài viết do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while creating news article.");

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync([FromBody] UpdateNewsArticleInputModel input, CancellationToken cancellationToken)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.NewsArticleId))
        {
            return new JsonResult(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin nhập liệu.",
                errors = ValidationResponseHelper.ExtractModelStateErrors(ModelState)
            });
        }

        try
        {
            var request = new UpdateNewsArticleApiModel
            {
                NewsTitle = string.IsNullOrWhiteSpace(input.NewsTitle) ? null : input.NewsTitle.Trim(),
                Headline = input.Headline.Trim(),
                NewsContent = string.IsNullOrWhiteSpace(input.NewsContent) ? null : input.NewsContent.Trim(),
                NewsSource = string.IsNullOrWhiteSpace(input.NewsSource) ? null : input.NewsSource.Trim(),
                CategoryId = input.CategoryId,
                NewsStatus = input.NewsStatus,
                TagIds = input.TagIds ?? new List<int>()
            };

            var updatedArticle = await _newsClientService.UpdateNewsArticleAsync(input.NewsArticleId, request, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = $"Cập nhật bài viết \"{(string.IsNullOrWhiteSpace(updatedArticle.NewsTitle) ? updatedArticle.Headline : updatedArticle.NewsTitle)}\" thành công.",
                article = updatedArticle
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while updating news article {Id}: {Message}", input.NewsArticleId, ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể cập nhật bài viết do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while updating news article {Id}.", input.NewsArticleId);

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteNewsArticleInputModel input, CancellationToken cancellationToken)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.NewsArticleId))
        {
            return new JsonResult(new { success = false, message = "Mã bài viết không hợp lệ." });
        }

        try
        {
            await _newsClientService.DeleteNewsArticleAsync(input.NewsArticleId, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = $"Đã xóa bài viết mã '{input.NewsArticleId}' thành công.",
                deletedId = input.NewsArticleId
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while deleting news article {Id}: {Message}", input.NewsArticleId, ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể xóa bài viết. Vui lòng thử lại."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while deleting news article {Id}.", input.NewsArticleId);

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống. Vui lòng thử lại sau."
            });
        }
    }
}

public class CreateNewsArticleInputModel
{
    [StringLength(400, ErrorMessage = "Tiêu đề bài viết không được vượt quá 400 ký tự.")]
    public string? NewsTitle { get; set; }

    [Required(ErrorMessage = "Tiêu đề tóm tắt (Headline) là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tiêu đề tóm tắt không được vượt quá 150 ký tự.")]
    public string Headline { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Nội dung bài viết không được vượt quá 4000 ký tự.")]
    public string? NewsContent { get; set; }

    [StringLength(400, ErrorMessage = "Nguồn tin không được vượt quá 400 ký tự.")]
    public string? NewsSource { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chuyên mục cho bài viết.")]
    [Range(1, short.MaxValue, ErrorMessage = "Vui lòng chọn một chuyên mục hợp lệ.")]
    public short CategoryId { get; set; }

    public bool NewsStatus { get; set; } = true;

    public List<int> TagIds { get; set; } = new();
}

public class UpdateNewsArticleInputModel
{
    [Required(ErrorMessage = "Mã bài viết là bắt buộc.")]
    public string NewsArticleId { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Tiêu đề bài viết không được vượt quá 400 ký tự.")]
    public string? NewsTitle { get; set; }

    [Required(ErrorMessage = "Tiêu đề tóm tắt (Headline) là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Tiêu đề tóm tắt không được vượt quá 150 ký tự.")]
    public string Headline { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Nội dung bài viết không được vượt quá 4000 ký tự.")]
    public string? NewsContent { get; set; }

    [StringLength(400, ErrorMessage = "Nguồn tin không được vượt quá 400 ký tự.")]
    public string? NewsSource { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chuyên mục cho bài viết.")]
    [Range(1, short.MaxValue, ErrorMessage = "Vui lòng chọn một chuyên mục hợp lệ.")]
    public short CategoryId { get; set; }

    public bool NewsStatus { get; set; } = true;

    public List<int> TagIds { get; set; } = new();
}

public class DeleteNewsArticleInputModel
{
    [Required(ErrorMessage = "Mã bài viết là bắt buộc.")]
    public string NewsArticleId { get; set; } = string.Empty;
}

