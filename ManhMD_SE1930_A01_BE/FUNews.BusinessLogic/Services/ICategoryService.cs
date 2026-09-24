using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Services;

public interface ICategoryService
{
    IQueryable<CategoryDto> GetQueryable();
    Task<CategoryDto?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
}
