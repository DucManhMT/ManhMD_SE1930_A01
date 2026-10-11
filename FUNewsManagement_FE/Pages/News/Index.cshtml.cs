using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.News;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly INewsClientService _newsClientService;
    private readonly ICategoryClientService _categoryClientService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        INewsClientService newsClientService,
        ICategoryClientService categoryClientService,
        ILogger<IndexModel> logger)
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
        // 1. Tải danh mục hoạt động cho dropdown bộ lọc
        try
        {
            var catEnvelope = await _categoryClientService.GetCategoriesAsync("$filter=isActive eq true&$orderby=categoryName asc&$top=100", cancellationToken);
            Categories = catEnvelope?.Value ?? new List<CategoryApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load categories for public news filter.");
            Categories = new List<CategoryApiModel>();
        }

        if (PageIndex < 1) PageIndex = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 50) PageSize = 50;

        // 2. Xây dựng OData query cho danh sách tin công khai (FUN-016: AC 1 - Không cần login; AC 3 - Không lộ inactive qua count)
        try
        {
            int skip = (PageIndex - 1) * PageSize;
            var odataQuery = ODataFilterHelper.BuildNewsQuery(
                searchTerm: SearchTerm,
                categoryId: CategoryId,
                statusFilter: "active", // Đảm bảo chỉ lấy tin Active
                authorTerm: null,
                startDate: null,
                endDate: null,
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
            _logger.LogWarning(ex, "API error fetching public news articles: {Message}", ex.Message);
            ErrorMessage = "Không thể kết nối đến máy chủ lấy danh sách tin tức. Vui lòng thử lại sau.";
            NewsArticles = new List<NewsArticleApiModel>();
            TotalCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching public news articles.");
            ErrorMessage = "Đã xảy ra lỗi không mong muốn khi tải trang tin tức.";
            NewsArticles = new List<NewsArticleApiModel>();
            TotalCount = 0;
        }
    }
}
