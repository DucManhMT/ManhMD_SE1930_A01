using System.ComponentModel.DataAnnotations;
using FUNews.Client.BusinessLogic.Helpers;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel : PageModel
{
    private readonly IAccountClientService _accountClientService;
    private readonly ILogger<AccountsModel> _logger;

    public AccountsModel(IAccountClientService accountClientService, ILogger<AccountsModel> logger)
    {
        _accountClientService = accountClientService ?? throw new ArgumentNullException(nameof(accountClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<AccountApiModel> Accounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoleFilter { get; set; }

    public int TotalCount { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public class CreateAccountInputModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(70, ErrorMessage = "Email không được vượt quá 70 ký tự.")]
        public string AccountEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        [Range(1, 2, ErrorMessage = "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).")]
        public int? AccountRole { get; set; }

        [Required(ErrorMessage = "Mật khẩu khởi tạo là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự.")]
        public string AccountPassword { get; set; } = string.Empty;
    }

    public class UpdateAccountInputModel
    {
        [Required(ErrorMessage = "Mã tài khoản là bắt buộc.")]
        public short AccountId { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(70, ErrorMessage = "Email không được vượt quá 70 ký tự.")]
        public string AccountEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        [Range(1, 2, ErrorMessage = "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).")]
        public int? AccountRole { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAccountsAsync();
    }

    public async Task<IActionResult> OnGetListAsync(string? searchTerm, int? roleFilter)
    {
        SearchTerm = searchTerm;
        RoleFilter = roleFilter;
        await LoadAccountsAsync();

        return new JsonResult(new
        {
            success = true,
            accounts = Accounts,
            totalCount = TotalCount
        });
    }

    public async Task<IActionResult> OnGetCheckEmailAsync(string? email, short? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new JsonResult(new { isDuplicate = false, checkFailed = false });
        }

        var query = ODataFilterHelper.BuildEmailUniquenessQuery(email, excludeId);

        try
        {
            var envelope = await _accountClientService.GetAccountsAsync(query, cancellationToken);
            var isDuplicate = (envelope.Count ?? 0) > 0 || (envelope.Value?.Count ?? 0) > 0;
            return new JsonResult(new { isDuplicate, checkFailed = false });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CheckEmail: Failed to verify uniqueness for email {Email}", email);
            return new JsonResult(new { isDuplicate = false, checkFailed = true });
        }
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateAccountInputModel input)
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

        if (!input.AccountRole.HasValue || (input.AccountRole.Value != 1 && input.AccountRole.Value != 2))
        {
            return new JsonResult(new
            {
                success = false,
                message = "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).",
                errors = new Dictionary<string, string[]>
                {
                    { "AccountRole", new[] { "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2)." } }
                }
            });
        }

        try
        {
            var request = new CreateAccountApiModel
            {
                AccountName = input.AccountName.Trim(),
                AccountEmail = input.AccountEmail.Trim(),
                AccountRole = input.AccountRole.Value,
                AccountPassword = input.AccountPassword
            };

            var createdAccount = await _accountClientService.CreateAccountAsync(request, HttpContext.RequestAborted);

            return new JsonResult(new
            {
                success = true,
                message = $"Tạo tài khoản thành công cho {createdAccount.AccountName} ({createdAccount.AccountEmail}).",
                account = createdAccount
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while creating account: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể tạo tài khoản do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while creating account.");
            return new JsonResult(new
            {
                success = false,
                message = "Không thể kết nối đến máy chủ Backend API. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnGetAccountAsync(short id, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountClientService.GetByIdAsync(id, cancellationToken);
            if (account == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Không tìm thấy tài khoản với mã #{id}."
                });
            }

            return new JsonResult(new
            {
                success = true,
                account
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve account #{Id}", id);
            return new JsonResult(new
            {
                success = false,
                message = "Không thể lấy thông tin tài khoản từ máy chủ."
            });
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync([FromBody] UpdateAccountInputModel input)
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

        if (!input.AccountRole.HasValue || (input.AccountRole.Value != 1 && input.AccountRole.Value != 2))
        {
            return new JsonResult(new
            {
                success = false,
                message = "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).",
                errors = new Dictionary<string, string[]>
                {
                    { "AccountRole", new[] { "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2)." } }
                }
            });
        }

        try
        {
            var request = new UpdateAccountApiModel
            {
                AccountName = input.AccountName.Trim(),
                AccountEmail = input.AccountEmail.Trim(),
                AccountRole = input.AccountRole.Value
            };

            var updatedAccount = await _accountClientService.UpdateAccountAsync(input.AccountId, request, HttpContext.RequestAborted);

            return new JsonResult(new
            {
                success = true,
                message = $"Cập nhật tài khoản thành công cho {updatedAccount.AccountName} ({updatedAccount.AccountEmail}).",
                account = updatedAccount
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while updating account #{Id}: {Message}", input.AccountId, ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể cập nhật tài khoản do lỗi dữ liệu từ hệ thống.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while updating account #{Id}", input.AccountId);
            return new JsonResult(new
            {
                success = false,
                message = "Không thể kết nối đến máy chủ Backend API. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(short id)
    {
        if (id <= 0 && Request.HasFormContentType && short.TryParse(Request.Form["id"], out var formId))
        {
            id = formId;
        }

        if (id <= 0)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Mã tài khoản cần xóa không hợp lệ."
            });
        }

        try
        {
            await _accountClientService.DeleteAccountAsync(id, HttpContext.RequestAborted);

            return new JsonResult(new
            {
                success = true,
                message = $"Đã xóa tài khoản #{id} thành công."
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while deleting account #{Id}: {Message}", id, ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể xóa tài khoản."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while deleting account #{Id}", id);
            return new JsonResult(new
            {
                success = false,
                message = "Không thể kết nối đến máy chủ Backend API. Vui lòng thử lại sau."
            });
        }
    }

    private async Task LoadAccountsAsync()
    {
        try
        {
            var query = ODataFilterHelper.BuildAccountsQuery(SearchTerm, RoleFilter);
            var envelope = await _accountClientService.GetAccountsAsync(query, HttpContext.RequestAborted);
            Accounts = envelope.Value ?? new List<AccountApiModel>();
            TotalCount = (int)(envelope.Count ?? Accounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load accounts from API.");
            ErrorMessage = "Không thể tải danh sách tài khoản từ máy chủ Backend API.";
            Accounts = new List<AccountApiModel>();
            TotalCount = 0;
        }
    }
}
