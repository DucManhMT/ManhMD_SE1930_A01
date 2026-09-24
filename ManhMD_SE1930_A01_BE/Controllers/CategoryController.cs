using FUNews.BusinessLogic.DTOs;
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
        var result = ODataQueryHelper.ApplyOData(_categoryService.GetQueryable(), queryOptions);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryDto>> GetById(short id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, cancellationToken);
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
    public IActionResult Create()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Chưa triển khai",
            Detail = "Chức năng tạo danh mục thuộc phạm vi task FUN-008."
        });
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
