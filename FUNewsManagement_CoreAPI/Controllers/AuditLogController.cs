using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogController(IAuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogPagedResponseDto>> Get(
        [FromQuery] int? userId,
        [FromQuery] string? entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditService.GetAuditLogsAsync(userId, entityType, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
