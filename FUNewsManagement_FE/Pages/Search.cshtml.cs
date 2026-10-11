using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages;

[AllowAnonymous]
public class SearchModel : PageModel
{
    private readonly INewsClientService _newsClientService;
    private readonly ICategoryClientService _categoryClientService;
    private readonly ITagClientService _tagClientService;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(
        INewsClientService newsClientService,
        ICategoryClientService categoryClientService,
        ITagClientService tagClientService,
        ILogger<SearchModel> logger)
    {
        _newsClientService = newsClientService ?? throw new ArgumentNullException(nameof(newsClientService));
        _categoryClientService = categoryClientService ?? throw new ArgumentNullException(nameof(categoryClientService));
        _tagClientService = tagClientService ?? throw new ArgumentNullException(nameof(tagClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public short? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TagId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AuthorTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public List<NewsArticleApiModel> NewsArticles { get; set; } = new();
    public List<CategoryApiModel> Categories { get; set; } = new();
    public List<TagApiModel> Tags { get; set; } = new();
    public int TotalCount { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsStaff { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // 1. Xác định quyền: chỉ Staff/Admin mới được xem bài Inactive (FUN-017: AC 1, AC 4)
        IsStaff = User.Identity?.IsAuthenticated == true && (User.IsInRole("Staff") || User.IsInRole("Admin"));

        // 2. Nạp danh sách chuyên mục hoạt động và thẻ tin cho bộ lọc
        await LoadFilterOptionsAsync(cancellationToken);

        // 3. Kiểm tra tính hợp lệ của khoảng ngày
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            ErrorMessage = "Khoảng ngày không hợp lệ: Ngày bắt đầu không thể lớn hơn Ngày kết thúc.";
            return;
        }

        // 4. Chuẩn hóa tham số phân trang
        if (PageIndex < 1) PageIndex = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 50) PageSize = 50;

        // 5. Giải quyết trạng thái: Anonymous và Lecturer luôn bị ép chỉ lấy tin Active (AC 4)
        string? effectiveStatusFilter;
        if (!IsStaff)
        {
            effectiveStatusFilter = "active";
        }
        else
        {
            effectiveStatusFilter = string.IsNullOrWhiteSpace(StatusFilter) ? null : StatusFilter;
        }

        // 6. Xây dựng OData query và gửi trực tiếp xuống API (AC 1, AC 2, AC 6: Không tải toàn bộ rồi lọc)
        try
        {
            int skip = (PageIndex - 1) * PageSize;
            var odataQuery = ODataFilterHelper.BuildNewsQuery(
                searchTerm: SearchTerm,
                categoryId: CategoryId,
                statusFilter: effectiveStatusFilter,
                authorTerm: AuthorTerm,
                startDate: StartDate,
                endDate: EndDate,
                sortBy: SortBy,
                top: PageSize,
                skip: skip,
                tagId: TagId,
                includeContent: true // AC 1: Tìm kiếm cả trên title, headline và content
            );

            var envelope = await _newsClientService.GetNewsArticlesAsync(odataQuery, cancellationToken);
            NewsArticles = envelope?.Value ?? new List<NewsArticleApiModel>();
            TotalCount = (int)(envelope?.Count ?? NewsArticles.Count);
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogError(ex, "API Error while searching news articles with query");
            ErrorMessage = "Không thể tìm kiếm bài viết do lỗi từ hệ thống API: " + ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while searching news articles");
            ErrorMessage = "Đã xảy ra lỗi không mong muốn khi tìm kiếm tin tức. Vui lòng thử lại sau.";
        }
    }

    private async Task LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var catTask = _categoryClientService.GetCategoriesAsync("$filter=isActive eq true&$orderby=categoryName asc&$top=100", cancellationToken);
            var tagTask = _tagClientService.GetTagsAsync("$orderby=tagName asc&$top=100", cancellationToken);

            await Task.WhenAll(catTask, tagTask);

            Categories = (await catTask)?.Value ?? new List<CategoryApiModel>();
            Tags = (await tagTask)?.Value ?? new List<TagApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load categories or tags for search options.");
            if (Categories == null || Categories.Count == 0) Categories = new List<CategoryApiModel>();
            if (Tags == null || Tags.Count == 0) Tags = new List<TagApiModel>();
        }
    }
}
