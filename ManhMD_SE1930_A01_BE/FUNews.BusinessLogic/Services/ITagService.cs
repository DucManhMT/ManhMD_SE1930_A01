using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Services;

public interface ITagService
{
    IQueryable<TagDto> GetQueryable();
    Task<TagDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
