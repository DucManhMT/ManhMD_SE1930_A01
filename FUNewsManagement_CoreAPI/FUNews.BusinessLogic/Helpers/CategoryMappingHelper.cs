using System.Linq.Expressions;
using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class CategoryMappingHelper
{
    public static readonly Expression<Func<Category, CategoryDto>> ProjectToDto = GetProjectToDto(false);

    public static Expression<Func<Category, CategoryDto>> GetProjectToDto(bool isStaff = false)
    {
        if (isStaff)
        {
            return c => new CategoryDto
            {
                CategoryId = c.CategoryID,
                CategoryName = c.CategoryName,
                CategoryDescription = c.CategoryDescription,
                ParentCategoryId = c.ParentCategoryID,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
                IsActive = c.IsActive,
                ArticleCount = c.NewsArticles.Count()
            };
        }

        return c => new CategoryDto
        {
            CategoryId = c.CategoryID,
            CategoryName = c.CategoryName,
            CategoryDescription = c.CategoryDescription,
            ParentCategoryId = c.ParentCategoryID,
            ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null,
            IsActive = c.IsActive,
            ArticleCount = c.NewsArticles.Count(a => a.NewsStatus == true)
        };
    }

    public static CategoryDto ToDto(Category category, bool isStaff = false)
    {
        ArgumentNullException.ThrowIfNull(category);

        int count = 0;
        if (category.NewsArticles != null)
        {
            count = isStaff
                ? category.NewsArticles.Count
                : category.NewsArticles.Count(a => a.NewsStatus == true);
        }

        return new CategoryDto
        {
            CategoryId = category.CategoryID,
            CategoryName = category.CategoryName,
            CategoryDescription = category.CategoryDescription,
            ParentCategoryId = category.ParentCategoryID,
            ParentCategoryName = category.ParentCategory?.CategoryName,
            IsActive = category.IsActive,
            ArticleCount = count
        };
    }
}
