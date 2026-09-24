using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Services;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace ManhMD_SE1930_A01_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagController(ITagService tagService)
    {
        _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ODataResponse<TagDto>> Get(ODataQueryOptions<TagDto> queryOptions)
    {
        var result = ODataQueryHelper.ApplyOData(_tagService.GetQueryable(), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TagDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var tag = await _tagService.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy thẻ",
                Detail = $"Thẻ với mã {id} không tồn tại."
            });
        }

        return Ok(tag);
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public IActionResult Create()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng tạo thẻ thuộc phạm vi task FUN-010."
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Staff")]
    public IActionResult Update(int id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng cập nhật thẻ thuộc phạm vi task FUN-010."
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Staff")]
    public IActionResult Delete(int id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng xóa thẻ thuộc phạm vi task FUN-010."
        });
    }
}
