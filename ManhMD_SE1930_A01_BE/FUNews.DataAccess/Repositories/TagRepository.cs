using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public class TagRepository : ITagRepository
{
    private readonly TagDAO _tagDao;

    public TagRepository(TagDAO tagDao)
    {
        _tagDao = tagDao ?? throw new ArgumentNullException(nameof(tagDao));
    }

    public IQueryable<Tag> GetQueryable()
    {
        return _tagDao.GetQueryable();
    }

    public Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _tagDao.GetByIdAsync(id, cancellationToken);
    }

    public Task<List<Tag>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        return _tagDao.GetByIdsAsync(ids, cancellationToken);
    }

    public Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _tagDao.GetAllAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _tagDao.ExistsAsync(id, cancellationToken);
    }

    public Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return _tagDao.IsNameUniqueAsync(name, excludeId, cancellationToken);
    }

    public Task<bool> HasArticlesAsync(int id, CancellationToken cancellationToken = default)
    {
        return _tagDao.HasArticlesAsync(id, cancellationToken);
    }

    public Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        return _tagDao.AddAsync(tag, cancellationToken);
    }

    public Task<Tag> UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        return _tagDao.UpdateAsync(tag, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _tagDao.DeleteAsync(id, cancellationToken);
    }
}
