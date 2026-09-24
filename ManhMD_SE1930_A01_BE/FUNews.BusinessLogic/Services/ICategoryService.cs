using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;

namespace FUNews.BusinessLogic.Services;

public interface ICategoryService
{
    IQueryable<CategoryDto> GetQueryable(bool isStaff = false);
    Task<CategoryDto?> GetByIdAsync(short id, bool isStaff = false, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default);
}
