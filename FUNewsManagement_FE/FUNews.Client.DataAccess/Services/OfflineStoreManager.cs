using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FUNews.Client.DataAccess.Services;

public class OfflineStoreManager : IOfflineStoreManager
{
    private readonly string _snapshotFilePath;
    private readonly ILogger<OfflineStoreManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private OfflineDataSnapshot? _memoryCache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SnapshotFilePath => _snapshotFilePath;

    public OfflineStoreManager(IConfiguration? configuration = null, ILogger<OfflineStoreManager>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OfflineStoreManager>.Instance;

        var configuredPath = configuration?["OfflineStore:SnapshotPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            _snapshotFilePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredPath);
        }
        else
        {
            _snapshotFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "cache", "snapshot.json");
        }
    }

    // Constructor phục vụ testing linh hoạt
    public OfflineStoreManager(string snapshotFilePath, ILogger<OfflineStoreManager>? logger = null)
    {
        _snapshotFilePath = snapshotFilePath ?? throw new ArgumentNullException(nameof(snapshotFilePath));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OfflineStoreManager>.Instance;
    }

    public async Task SaveSnapshotAsync(OfflineDataSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_snapshotFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(_snapshotFilePath, json, cancellationToken);

            _memoryCache = snapshot;
            _logger.LogInformation("Saved offline snapshot to {Path} successfully with {ArticlesCount} articles, {CategoriesCount} categories, {TagsCount} tags.",
                _snapshotFilePath, snapshot.Articles.Count, snapshot.Categories.Count, snapshot.Tags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save offline snapshot to {Path}.", _snapshotFilePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<OfflineDataSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_memoryCache != null)
        {
            return _memoryCache;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache != null)
            {
                return _memoryCache;
            }

            if (!File.Exists(_snapshotFilePath))
            {
                _logger.LogWarning("Offline snapshot file not found at {Path}.", _snapshotFilePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_snapshotFilePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var snapshot = JsonSerializer.Deserialize<OfflineDataSnapshot>(json, JsonOptions);
            _memoryCache = snapshot;
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read offline snapshot from {Path}.", _snapshotFilePath);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public OfflineDataSnapshot? GetCachedSnapshot()
    {
        return _memoryCache;
    }

    public bool HasSnapshot()
    {
        return _memoryCache != null || File.Exists(_snapshotFilePath);
    }

    private volatile bool _isOffline;

    public bool IsOffline
    {
        get => _isOffline;
        set => _isOffline = value;
    }

    public void MarkApiFailure()
    {
        if (!_isOffline)
        {
            _isOffline = true;
            _logger.LogWarning("System marked as OFFLINE due to API connectivity failure.");
        }
    }

    public void MarkApiSuccess()
    {
        if (_isOffline)
        {
            _isOffline = false;
            _logger.LogInformation("System marked as ONLINE. API connectivity restored.");
        }
    }
}
