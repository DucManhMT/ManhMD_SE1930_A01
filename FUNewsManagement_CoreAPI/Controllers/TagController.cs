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
        bool isStaff = User.IsInRole("Staff");
        var result = ODataQueryHelper.ApplyOData(_tagService.GetQueryable(isStaff), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TagDto>> GetById(int id, CancellationToken cancellationToken)
    {
        bool isStaff = User.IsInRole("Staff");
        var tag = await _tagService.GetByIdAsync(id, isStaff, cancellationToken);
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

    [HttpGet("{id:int}/news")]
    [AllowAnonymous]
    public async Task<ActionResult<List<NewsArticleDto>>> GetNewsByTag(int id, CancellationToken cancellationToken)
    {
        bool isStaff = User.IsInRole("Staff");
        var articles = await _tagService.GetArticlesByTagAsync(id, isStaff, cancellationToken);
        return Ok(articles);
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<TagDto>> Create([FromBody] CreateTagRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _tagService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.TagId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<TagDto>> Update(int id, [FromBody] UpdateTagRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await _tagService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _tagService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
