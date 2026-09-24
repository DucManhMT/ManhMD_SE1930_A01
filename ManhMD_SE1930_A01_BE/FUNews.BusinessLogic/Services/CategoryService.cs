using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Helpers;
using FUNews.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public IQueryable<CategoryDto> GetQueryable()
    {
        return _categoryRepository.GetQueryable()
            .AsNoTracking()
            .Select(CategoryMappingHelper.ProjectToDto);
    }

    public async Task<CategoryDto?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdWithParentAndChildrenAsync(id, cancellationToken);
        return category != null ? CategoryMappingHelper.ToDto(category) : null;
    }
}
