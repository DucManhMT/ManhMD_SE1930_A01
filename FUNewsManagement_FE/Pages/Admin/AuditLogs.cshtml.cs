using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ManhMD_SE1930_A01_FE.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuditLogsModel : PageModel
{
    private readonly IAuditLogClientService _auditLogService;
    private readonly IAccountClientService _accountService;
    private readonly ILogger<AuditLogsModel> _logger;

    public AuditLogsModel(
        IAuditLogClientService auditLogService,
        IAccountClientService accountService,
        ILogger<AuditLogsModel> logger)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AuditLogPagedResponseApiModel PagedResult { get; set; } = new();

    public List<AccountApiModel> AccountsList { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageIndex < 1) PageIndex = 1;

        try
        {
            var accountsEnvelope = await _accountService.GetAccountsAsync(cancellationToken: cancellationToken);
            AccountsList = accountsEnvelope?.Value ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load accounts for audit log filter dropdown.");
        }

        try
        {
            PagedResult = await _auditLogService.GetAuditLogsAsync(
                UserId,
                EntityType,
                PageIndex,
                pageSize: 15,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load audit logs.");
            ErrorMessage = "Không thể tải danh sách nhật ký thay đổi dữ liệu từ hệ thống. Vui lòng thử lại.";
        }
    }
}
