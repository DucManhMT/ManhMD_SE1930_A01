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
public class TagsModel : PageModel
{
    private readonly ITagClientService _tagClientService;
    private readonly ILogger<TagsModel> _logger;

    public TagsModel(ITagClientService tagClientService, ILogger<TagsModel> logger)
    {
        _tagClientService = tagClientService ?? throw new ArgumentNullException(nameof(tagClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<TagApiModel> Tags { get; set; } = new();

    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    public string? ErrorMessage { get; set; }

    public class CreateTagInputModel
    {
        [Required(ErrorMessage = "Tên thẻ tin là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Tên thẻ tin không được vượt quá 50 ký tự.")]
        public string TagName { get; set; } = string.Empty;

        [StringLength(400, ErrorMessage = "Ghi chú thẻ tin không được vượt quá 400 ký tự.")]
        public string? Note { get; set; }
    }

    public class UpdateTagInputModel
    {
        [Required(ErrorMessage = "Mã thẻ tin là bắt buộc.")]
        public int TagId { get; set; }

        [Required(ErrorMessage = "Tên thẻ tin là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Tên thẻ tin không được vượt quá 50 ký tự.")]
        public string TagName { get; set; } = string.Empty;

        [StringLength(400, ErrorMessage = "Ghi chú thẻ tin không được vượt quá 400 ký tự.")]
        public string? Note { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnGetListAsync(string? search, CancellationToken cancellationToken)
    {
        try
        {
            var query = BuildODataQuery(search);
            var envelope = await _tagClientService.GetTagsAsync(query, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                tags = envelope.Value ?? new List<TagApiModel>(),
                totalCount = (int)(envelope.Count ?? envelope.Value?.Count ?? 0)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tags via AJAX.");
            return new JsonResult(new { success = false, message = "Không thể tải danh sách thẻ tin tức." });
        }
    }

    public async Task<IActionResult> OnGetArticlesAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var articles = await _tagClientService.GetArticlesByTagAsync(id, cancellationToken);
            return new JsonResult(new
            {
                success = true,
                articles
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error fetching articles for tag {TagId}: {Message}", id, ex.Message);
            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể tải danh sách bài viết gắn thẻ."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching articles for tag {TagId}", id);
            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi khi lấy danh sách bài viết."
            });
        }
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateTagInputModel input, CancellationToken cancellationToken)
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
            var request = new CreateTagApiModel
            {
                TagName = input.TagName.Trim(),
                Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim()
            };

            var createdTag = await _tagClientService.CreateTagAsync(request, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = $"Tạo thẻ tin \"{createdTag.TagName}\" thành công.",
                tag = createdTag
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while creating tag: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể tạo thẻ tin do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while creating tag.");

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync([FromBody] UpdateTagInputModel input, CancellationToken cancellationToken)
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
            var request = new UpdateTagApiModel
            {
                TagName = input.TagName.Trim(),
                Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim()
            };

            var updatedTag = await _tagClientService.UpdateTagAsync(input.TagId, request, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = $"Cập nhật thẻ tin \"{updatedTag.TagName}\" thành công.",
                tag = updatedTag
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while updating tag: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể cập nhật thẻ tin do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while updating tag.");

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _tagClientService.DeleteTagAsync(id, cancellationToken);

            return new JsonResult(new
            {
                success = true,
                message = "Xóa thẻ tin thành công."
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while deleting tag: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể xóa thẻ tin do ràng buộc dữ liệu."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while deleting tag.");

            return new JsonResult(new
            {
                success = false,
                message = "Đã xảy ra lỗi không mong muốn trên hệ thống khi xóa thẻ tin."
            });
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var query = BuildODataQuery(SearchKeyword);
            var envelope = await _tagClientService.GetTagsAsync(query);

            Tags = envelope.Value ?? new List<TagApiModel>();
            TotalCount = (int)(envelope.Count ?? Tags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tags on page load.");
            ErrorMessage = "Không thể tải danh sách thẻ tin tức từ máy chủ. Vui lòng kiểm tra lại dịch vụ Backend.";
            Tags = new List<TagApiModel>();
            TotalCount = 0;
        }
    }

    private static string BuildODataQuery(string? search)
    {
        return ODataFilterHelper.BuildTagsQuery(search);
    }
}
