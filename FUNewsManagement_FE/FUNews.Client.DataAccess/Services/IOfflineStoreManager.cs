using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Services;

public interface IOfflineStoreManager
{
    Task SaveSnapshotAsync(OfflineDataSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<OfflineDataSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);
    OfflineDataSnapshot? GetCachedSnapshot();
    bool HasSnapshot();
    string SnapshotFilePath { get; }
    bool IsOffline { get; set; }
    void MarkApiFailure();
    void MarkApiSuccess();
}
