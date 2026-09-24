using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Staff;

[Authorize(Roles = "Staff")]
public class NewsModel : PageModel
{
    private readonly INewsClientService _newsClientService;
    private readonly ICategoryClientService _categoryClientService;
    private readonly ILogger<NewsModel> _logger;

    public NewsModel(
        INewsClientService newsClientService,
        ICategoryClientService categoryClientService,
        ILogger<NewsModel> logger)
    {
        _newsClientService = newsClientService ?? throw new ArgumentNullException(nameof(newsClientService));
        _categoryClientService = categoryClientService ?? throw new ArgumentNullException(nameof(categoryClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<NewsArticleApiModel> NewsArticles { get; set; } = new();
    public List<CategoryApiModel> Categories { get; set; } = new();

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
        // 1. Tải danh mục phục vụ dropdown bộ lọc
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

        // 2. Validate ngày hợp lệ (AC 3)
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

        // 3. Xây dựng OData query và lấy dữ liệu bài viết
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
}
