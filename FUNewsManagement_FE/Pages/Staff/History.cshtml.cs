using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Staff;

[Authorize(Roles = "Staff")]
public class HistoryModel : PageModel
{
    private readonly INewsClientService _newsClientService;
    private readonly ICategoryClientService _categoryClientService;
    private readonly ILogger<HistoryModel> _logger;

    public HistoryModel(
        INewsClientService newsClientService,
        ICategoryClientService categoryClientService,
        ILogger<HistoryModel> logger)
    {
        _newsClientService = newsClientService ?? throw new ArgumentNullException(nameof(newsClientService));
        _categoryClientService = categoryClientService ?? throw new ArgumentNullException(nameof(categoryClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public short? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public List<NewsArticleApiModel> NewsArticles { get; set; } = new();
    public List<CategoryApiModel> Categories { get; set; } = new();
    public int TotalCount { get; set; }
    public string? ErrorMessage { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // 1. Tải danh sách categories cho dropdown bộ lọc
        try
        {
            var catEnvelope = await _categoryClientService.GetCategoriesAsync("$orderby=categoryName asc&$top=100", cancellationToken);
            Categories = catEnvelope?.Value ?? new List<CategoryApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load categories for history filter dropdown.");
            Categories = new List<CategoryApiModel>();
        }

        // 2. Validate ngày hợp lệ (Từ ngày <= Đến ngày)
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

        // 3. Xây dựng OData query và lấy dữ liệu bài viết cá nhân từ /api/news/mine
        try
        {
            int skip = (PageIndex - 1) * PageSize;
            var odataQuery = ODataFilterHelper.BuildNewsQuery(
                searchTerm: SearchTerm,
                categoryId: CategoryId,
                statusFilter: StatusFilter,
                authorTerm: null, // FUN-015: AC 1 - Không truyền tác giả từ client, server tự lấy từ token
                startDate: StartDate,
                endDate: EndDate,
                sortBy: SortBy,
                top: PageSize,
                skip: skip
            );

            var envelope = await _newsClientService.GetMyNewsArticlesAsync(odataQuery, cancellationToken);
            NewsArticles = envelope?.Value ?? new List<NewsArticleApiModel>();
            TotalCount = (int)(envelope?.Count ?? NewsArticles.Count);
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error fetching my news articles: {Message}", ex.Message);
            ErrorMessage = ex.Message ?? "Không thể kết nối đến máy chủ lấy lịch sử bài viết.";
            NewsArticles = new List<NewsArticleApiModel>();
            TotalCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching my news articles.");
            ErrorMessage = "Đã xảy ra lỗi không mong muốn khi tải lịch sử bài viết.";
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
                return new JsonResult(new { success = false, message = $"Không tìm thấy bài viết với mã '{id}'." });
            }

            return new JsonResult(new { success = true, article });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error fetching news article detail {Id}: {Message}", id, ex.Message);
            return new JsonResult(new { success = false, message = ex.Message ?? "Không thể tải chi tiết bài viết." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching news article detail {Id}.", id);
            return new JsonResult(new { success = false, message = "Đã xảy ra lỗi hệ thống khi tải chi tiết bài viết." });
        }
    }
}
