using FUNews.BusinessLogic.DTOs;
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
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryID,
                CategoryName = c.CategoryName,
                CategoryDescription = c.CategoryDescription,
                ParentCategoryId = c.ParentCategoryID,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
                IsActive = c.IsActive
            });
    }

    public async Task<CategoryDto?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdWithParentAndChildrenAsync(id, cancellationToken);
        if (category == null) return null;

        return new CategoryDto
        {
            CategoryId = category.CategoryID,
            CategoryName = category.CategoryName,
            CategoryDescription = category.CategoryDescription,
            ParentCategoryId = category.ParentCategoryID,
            ParentCategoryName = category.ParentCategory?.CategoryName,
            IsActive = category.IsActive
        };
    }
}
