using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Helpers;
using FUNews.BusinessLogic.Models;
using FUNews.DataAccess.Entities;
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

    public IQueryable<CategoryDto> GetQueryable(bool isStaff = false)
    {
        return _categoryRepository.GetQueryable()
            .AsNoTracking()
            .Select(CategoryMappingHelper.GetProjectToDto(isStaff));
    }

    public async Task<CategoryDto?> GetByIdAsync(short id, bool isStaff = false, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdWithParentAndChildrenAsync(id, cancellationToken);
        return category != null ? CategoryMappingHelper.ToDto(category, isStaff) : null;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedName = request.CategoryName?.Trim() ?? string.Empty;
        var trimmedDesc = request.CategoryDescription?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ValidationException(nameof(request.CategoryName), "Tên chuyên mục là bắt buộc.");
        }

        if (trimmedName.Length > 100)
        {
            throw new ValidationException(nameof(request.CategoryName), "Tên chuyên mục không được vượt quá 100 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(trimmedDesc))
        {
            throw new ValidationException(nameof(request.CategoryDescription), "Mô tả chuyên mục là bắt buộc.");
        }

        if (trimmedDesc.Length > 250)
        {
            throw new ValidationException(nameof(request.CategoryDescription), "Mô tả chuyên mục không được vượt quá 250 ký tự.");
        }

        // Chặn self/cycle và kiểm tra danh mục cha nếu có chỉ định
        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _categoryRepository.ExistsAsync(request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new ValidationException(nameof(request.ParentCategoryId), "Danh mục cha không tồn tại trong hệ thống.");
            }
        }

        // Kiểm tra tính duy nhất cùng parent (kể cả parent == null)
        var isUnique = await _categoryRepository.IsNameUniqueAsync(trimmedName, request.ParentCategoryId, null, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.CategoryName), "Tên danh mục đã tồn tại trong cùng danh mục cha.");
        }

        var category = new Category
        {
            CategoryName = trimmedName,
            CategoryDescription = trimmedDesc,
            ParentCategoryID = request.ParentCategoryId,
            IsActive = request.IsActive ?? true
        };

        try
        {
            var created = await _categoryRepository.AddAsync(category, cancellationToken);
            if (created.ParentCategoryID.HasValue)
            {
                var parent = await _categoryRepository.GetByIdAsync(created.ParentCategoryID.Value, cancellationToken);
                created.ParentCategory = parent;
            }

            return CategoryMappingHelper.ToDto(created, isStaff: true);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("UQ_Category_Name") == true)
            {
                throw new ValidationException(nameof(request.CategoryName), "Tên danh mục đã tồn tại trong cùng danh mục cha.");
            }
            throw;
        }
    }
}
