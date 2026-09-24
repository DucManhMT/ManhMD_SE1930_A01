using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Services;
using ManhMD_SE1930_A01_BE.OData;
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
    public ActionResult<ODataResponse<TagDto>> Get(ODataQueryOptions<TagDto> queryOptions)
    {
        var result = ODataQueryHelper.ApplyOData(_tagService.GetQueryable(), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
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
}
