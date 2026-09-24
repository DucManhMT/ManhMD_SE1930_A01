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
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ODataResponse<CategoryDto>> Get(ODataQueryOptions<CategoryDto> queryOptions)
    {
        bool isStaff = User.IsInRole("Staff");
        var result = ODataQueryHelper.ApplyOData(_categoryService.GetQueryable(isStaff), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryDto>> GetById(short id, CancellationToken cancellationToken)
    {
        bool isStaff = User.IsInRole("Staff");
        var category = await _categoryService.GetByIdAsync(id, isStaff, cancellationToken);
        if (category == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy danh mục",
                Detail = $"Danh mục với mã {id} không tồn tại."
            });
        }

        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.CategoryId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Staff")]
    public IActionResult Update(short id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng cập nhật danh mục thuộc phạm vi task FUN-009."
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Staff")]
    public IActionResult Delete(short id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng xóa danh mục thuộc phạm vi task FUN-009."
        });
    }
}
