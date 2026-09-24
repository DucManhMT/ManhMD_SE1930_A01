using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages;

public class IndexModel : PageModel
{
    private readonly ICategoryClientService _categoryClientService;
    private readonly ILogger<IndexModel> _logger;

    public List<CategoryApiModel> ActiveCategories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public IndexModel(ICategoryClientService categoryClientService, ILogger<IndexModel> logger)
    {
        _categoryClientService = categoryClientService ?? throw new ArgumentNullException(nameof(categoryClientService));
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var result = await _categoryClientService.GetCategoriesAsync("$filter=isActive eq true&$orderby=categoryName&$top=10");
            ActiveCategories = result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load categories from backend API on home page.");
            ErrorMessage = "Chưa kết nối được tới dịch vụ Backend API hoặc hệ thống đang khởi động.";
        }
    }
}
