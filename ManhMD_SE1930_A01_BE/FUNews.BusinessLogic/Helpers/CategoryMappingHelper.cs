using System.Linq.Expressions;
using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class CategoryMappingHelper
{
    public static readonly Expression<Func<Category, CategoryDto>> ProjectToDto = c => new CategoryDto
    {
        CategoryId = c.CategoryID,
        CategoryName = c.CategoryName,
        CategoryDescription = c.CategoryDescription,
        ParentCategoryId = c.ParentCategoryID,
        ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
        IsActive = c.IsActive
    };

    public static CategoryDto ToDto(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

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
