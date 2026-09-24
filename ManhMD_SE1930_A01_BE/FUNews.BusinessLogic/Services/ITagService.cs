using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;

namespace FUNews.BusinessLogic.Services;

public interface ITagService
{
    IQueryable<TagDto> GetQueryable(bool isStaff = false);
    Task<TagDto?> GetByIdAsync(int id, bool isStaff = false, CancellationToken cancellationToken = default);
    Task<TagDto> CreateAsync(CreateTagRequestDto request, CancellationToken cancellationToken = default);
    Task<TagDto> UpdateAsync(int id, UpdateTagRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<NewsArticleDto>> GetArticlesByTagAsync(int tagId, bool isStaff = false, CancellationToken cancellationToken = default);
}
