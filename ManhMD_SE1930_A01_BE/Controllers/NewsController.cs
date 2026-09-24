using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Services;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsArticleService _newsService;

    public NewsController(INewsArticleService newsService)
    {
        _newsService = newsService ?? throw new ArgumentNullException(nameof(newsService));
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ODataResponse<NewsArticleDto>> Get(ODataQueryOptions<NewsArticleDto> queryOptions, [FromQuery] bool? activeOnly)
    {
        // Enforce role-based visibility: Public/Lecturer can only ever see Active articles
        var isPrivileged = User.Identity?.IsAuthenticated == true && (User.IsInRole("Staff") || User.IsInRole("Admin"));
        var filterActive = !isPrivileged || (activeOnly == true);

        var query = _newsService.GetQueryable(filterActive);
        var result = ODataQueryHelper.ApplyOData(query, queryOptions);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsArticleDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var isPrivileged = User.Identity?.IsAuthenticated == true && (User.IsInRole("Staff") || User.IsInRole("Admin"));
        var filterActive = !isPrivileged;

        var article = await _newsService.GetByIdAsync(id, filterActive, cancellationToken);
        if (article == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy bài viết",
                Detail = $"Bài viết với mã {id} không tồn tại hoặc bạn không có quyền xem."
            });
        }

        return Ok(article);
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<NewsArticleDto>> Create([FromBody] CreateNewsArticleRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Yêu cầu không hợp lệ",
                Detail = "Dữ liệu bài viết không được để trống."
            });
        }

        var accountId = GetCurrentAccountId();
        if (!accountId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Không xác định danh tính",
                Detail = "Không tìm thấy thông tin tài khoản nhân viên hợp lệ từ phiên đăng nhập."
            });
        }

        var created = await _newsService.CreateAsync(request, accountId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.NewsArticleId }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Staff")]
    public IActionResult Update(string id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng cập nhật bài viết thuộc phạm vi task FUN-013."
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff")]
    public IActionResult Delete(string id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng xóa bài viết thuộc phạm vi task FUN-013."
        });
    }

    [HttpPost("{id}/duplicate")]
    [Authorize(Roles = "Staff")]
    public IActionResult Duplicate(string id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng nhân bản bài viết thuộc phạm vi task FUN-014."
        });
    }

    private short? GetCurrentAccountId()
    {
        var claimVal = User.FindFirst("accountId")?.Value
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return short.TryParse(claimVal, out var id) ? id : null;
    }
}
