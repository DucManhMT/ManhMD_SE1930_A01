using System.ComponentModel.DataAnnotations;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
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

    /// <summary>
    /// Live server-side email uniqueness check for the creation modal blur handler.
    /// Queries the BE API via OData $filter eq so the check covers all accounts,
    /// not just the ≤100 rows visible in the table (M2 fix).
    /// Returns: { isDuplicate: bool, checkFailed: bool }
    /// </summary>
    public async Task<IActionResult> OnGetCheckEmailAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new JsonResult(new { isDuplicate = false, checkFailed = false });
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        // OData literal: escape single-quotes for safe query string inclusion.
        var escapedEmail = normalizedEmail.Replace("'", "''");
        var query = $"$filter=accountEmail eq '{escapedEmail}'&$top=1&$count=true";

        try
        {
            var envelope = await _accountClientService.GetAccountsAsync(query, cancellationToken);
            var isDuplicate = (envelope.Count ?? 0) > 0 || (envelope.Value?.Count ?? 0) > 0;
            return new JsonResult(new { isDuplicate, checkFailed = false });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CheckEmail: Failed to verify uniqueness for email {Email}", email);
            // Cannot check → do not block UI; server POST will catch any duplicate.
            return new JsonResult(new { isDuplicate = false, checkFailed = true });
        }
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateAccountInputModel input)
    {
        if (input == null)
        {
            return new JsonResult(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        // Validate ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => NormalizePropertyName(k.Key),
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return new JsonResult(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin nhập liệu.",
                errors
            });
        }

        // Client service validation for role
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

            var normalizedErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (ex.ValidationErrors != null)
            {
                foreach (var kvp in ex.ValidationErrors)
                {
                    normalizedErrors[NormalizePropertyName(kvp.Key)] = kvp.Value;
                }
            }

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể tạo tài khoản do lỗi dữ liệu từ hệ thống.",
                errors = normalizedErrors
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

    private async Task LoadAccountsAsync()
    {
        try
        {
            var query = BuildODataQuery(SearchTerm, RoleFilter);
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

    private static string BuildODataQuery(string? searchTerm, int? roleFilter)
    {
        var filters = new List<string>();

        if (roleFilter.HasValue && (roleFilter.Value == 1 || roleFilter.Value == 2))
        {
            filters.Add($"accountRole eq {roleFilter.Value}");
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var safeTerm = searchTerm.Trim().Replace("'", "''");
            filters.Add($"(contains(accountName,'{safeTerm}') or contains(accountEmail,'{safeTerm}'))");
        }

        var queryParts = new List<string>
        {
            "$orderby=accountId asc",
            "$count=true",
            "$top=100"
        };

        if (filters.Count > 0)
        {
            queryParts.Insert(0, $"$filter={string.Join(" and ", filters)}");
        }

        return string.Join("&", queryParts);
    }

    private static string NormalizePropertyName(string propertyName)
    {
        var clean = propertyName.Replace("input.", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("Input.", "", StringComparison.OrdinalIgnoreCase);

        if (clean.Equals("AccountEmail", StringComparison.OrdinalIgnoreCase) || clean.Equals("email", StringComparison.OrdinalIgnoreCase))
            return "AccountEmail";
        if (clean.Equals("AccountName", StringComparison.OrdinalIgnoreCase) || clean.Equals("name", StringComparison.OrdinalIgnoreCase))
            return "AccountName";
        if (clean.Equals("AccountRole", StringComparison.OrdinalIgnoreCase) || clean.Equals("role", StringComparison.OrdinalIgnoreCase))
            return "AccountRole";
        if (clean.Equals("AccountPassword", StringComparison.OrdinalIgnoreCase) || clean.Equals("password", StringComparison.OrdinalIgnoreCase))
            return "AccountPassword";

        return clean;
    }
}
