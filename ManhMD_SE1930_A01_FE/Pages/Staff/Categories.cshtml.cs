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
public class CategoriesModel : PageModel
{
    private readonly ICategoryClientService _categoryClientService;
    private readonly ILogger<CategoriesModel> _logger;

    public CategoriesModel(ICategoryClientService categoryClientService, ILogger<CategoriesModel> logger)
    {
        _categoryClientService = categoryClientService ?? throw new ArgumentNullException(nameof(categoryClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<CategoryApiModel> Categories { get; set; } = new();
    public List<CategoryApiModel> ParentCategories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public int TotalCount { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public class CreateCategoryInputModel
    {
        [Required(ErrorMessage = "Tên chuyên mục là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên chuyên mục không được vượt quá 100 ký tự.")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả chuyên mục là bắt buộc.")]
        [StringLength(250, ErrorMessage = "Mô tả chuyên mục không được vượt quá 250 ký tự.")]
        public string CategoryDescription { get; set; } = string.Empty;

        public short? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnGetListAsync(string? searchTerm, string? statusFilter)
    {
        SearchTerm = searchTerm;
        StatusFilter = statusFilter;
        await LoadDataAsync();

        return new JsonResult(new
        {
            success = true,
            categories = Categories,
            totalCount = TotalCount
        });
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateCategoryInputModel input, CancellationToken cancellationToken)
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
            var request = new CreateCategoryApiModel
            {
                CategoryName = input.CategoryName.Trim(),
                CategoryDescription = input.CategoryDescription.Trim(),
                ParentCategoryId = input.ParentCategoryId,
                IsActive = input.IsActive
            };

            var createdCategory = await _categoryClientService.CreateCategoryAsync(request, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = $"Tạo chuyên mục \"{createdCategory.CategoryName}\" thành công.",
                category = createdCategory
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while creating category: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể tạo chuyên mục do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while creating category.");

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống. Vui lòng thử lại sau."
            });
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var query = ODataFilterHelper.BuildCategoriesQuery(SearchTerm, StatusFilter);
            var result = await _categoryClientService.GetCategoriesAsync(query, HttpContext.RequestAborted);
            Categories = result.Value ?? new List<CategoryApiModel>();
            TotalCount = (int)(result.Count ?? Categories.Count);

            // Load parent categories for dropdown (active categories)
            var parentQuery = "$filter=isActive eq true&$orderby=categoryName asc";
            var parentResult = await _categoryClientService.GetCategoriesAsync(parentQuery, HttpContext.RequestAborted);
            ParentCategories = parentResult.Value ?? new List<CategoryApiModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories for staff management.");
            ErrorMessage = "Không thể tải danh sách chuyên mục lúc này. Vui lòng thử lại sau.";
            Categories = new List<CategoryApiModel>();
            ParentCategories = new List<CategoryApiModel>();
            TotalCount = 0;
        }
    }
}
