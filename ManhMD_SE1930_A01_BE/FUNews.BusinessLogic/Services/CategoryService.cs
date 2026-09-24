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

        var trimmedName = CategoryValidationHelper.ValidateAndTrimName(request.CategoryName);
        var trimmedDesc = CategoryValidationHelper.ValidateAndTrimDescription(request.CategoryDescription);

        await ValidateParentCategoryAsync(request.ParentCategoryId, currentCategoryId: null, cancellationToken);

        var isUnique = await _categoryRepository.IsNameUniqueAsync(trimmedName, request.ParentCategoryId, null, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.CategoryName), CategoryValidationHelper.CreateDuplicateNameMessage);
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
            CategoryValidationHelper.HandleDbUpdateException(ex, message: CategoryValidationHelper.CreateDuplicateNameMessage);
            throw;
        }
    }

    public async Task<CategoryDto> UpdateAsync(short id, UpdateCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new NotFoundException($"Chuyên mục với mã {id} không tồn tại.");
        }

        var trimmedName = CategoryValidationHelper.ValidateAndTrimName(request.CategoryName);
        var trimmedDesc = CategoryValidationHelper.ValidateAndTrimDescription(request.CategoryDescription);

        CategoryValidationHelper.ValidateParentNotSelf(request.ParentCategoryId, id);
        await ValidateParentCategoryAsync(request.ParentCategoryId, currentCategoryId: id, cancellationToken);
        await ValidateParentChangeAsync(existing, request.ParentCategoryId, cancellationToken);

        var isUnique = await _categoryRepository.IsNameUniqueAsync(trimmedName, request.ParentCategoryId, id, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.CategoryName), CategoryValidationHelper.DuplicateNameMessage);
        }

        existing.CategoryName = trimmedName;
        existing.CategoryDescription = trimmedDesc;
        existing.ParentCategoryID = request.ParentCategoryId;
        if (request.IsActive.HasValue)
        {
            existing.IsActive = request.IsActive.Value;
        }

        try
        {
            var updated = await _categoryRepository.UpdateAsync(existing, cancellationToken);
            var resultCategory = await _categoryRepository.GetByIdWithParentAndChildrenAsync(updated.CategoryID, cancellationToken);
            return CategoryMappingHelper.ToDto(resultCategory ?? updated, isStaff: true);
        }
        catch (DbUpdateException ex)
        {
            CategoryValidationHelper.HandleDbUpdateException(ex, message: CategoryValidationHelper.DuplicateNameMessage);
            throw;
        }
    }

    public async Task DeleteAsync(short id, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new NotFoundException($"Chuyên mục với mã {id} không tồn tại.");
        }

        await ValidateDeletableAsync(id, cancellationToken);
        await _categoryRepository.DeleteAsync(id, cancellationToken);
    }

    private async Task ValidateParentCategoryAsync(short? parentCategoryId, short? currentCategoryId, CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue)
        {
            return;
        }

        var parentExists = await _categoryRepository.ExistsAsync(parentCategoryId.Value, cancellationToken);
        if (!parentExists)
        {
            throw new ValidationException("ParentCategoryId", "Danh mục cha không tồn tại trong hệ thống.");
        }

        if (currentCategoryId.HasValue)
        {
            var isDescendant = await IsDescendantAsync(currentCategoryId.Value, parentCategoryId.Value, cancellationToken);
            if (isDescendant)
            {
                throw new ValidationException("ParentCategoryId", "Không thể chọn chuyên mục con làm danh mục cha (tránh tạo vòng lặp).");
            }
        }
    }

    private async Task ValidateParentChangeAsync(Category existing, short? newParentCategoryId, CancellationToken cancellationToken)
    {
        if (existing.ParentCategoryID != newParentCategoryId)
        {
            var hasArticles = await _categoryRepository.HasArticlesAsync(existing.CategoryID, cancellationToken);
            if (hasArticles)
            {
                throw new ValidationException("ParentCategoryId", "Không thể thay đổi danh mục cha của chuyên mục đã có bài viết.");
            }
        }
    }

    private async Task ValidateDeletableAsync(short categoryId, CancellationToken cancellationToken)
    {
        if (await _categoryRepository.HasArticlesAsync(categoryId, cancellationToken))
        {
            throw new ConflictException("Không thể xóa chuyên mục vì đang có bài viết liên kết.");
        }

        if (await _categoryRepository.HasChildrenAsync(categoryId, cancellationToken))
        {
            throw new ConflictException("Không thể xóa chuyên mục vì đang có chuyên mục con.");
        }
    }

    private async Task<bool> IsDescendantAsync(short rootId, short candidateId, CancellationToken cancellationToken)
    {
        var currentId = (short?)candidateId;
        var visited = new HashSet<short>();

        while (currentId.HasValue)
        {
            if (currentId.Value == rootId)
            {
                return true;
            }

            if (!visited.Add(currentId.Value))
            {
                break;
            }

            var parent = await _categoryRepository.GetByIdAsync(currentId.Value, cancellationToken);
            currentId = parent?.ParentCategoryID;
        }

        return false;
    }
}
