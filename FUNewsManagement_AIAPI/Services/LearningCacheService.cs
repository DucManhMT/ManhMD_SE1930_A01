using System.Collections.Concurrent;
using System.Text.Json;

namespace FUNewsManagement_AIAPI.Services;

public class LearningCacheService : ILearningCacheService
{
    private readonly ConcurrentDictionary<int, int> _cache = new();
    private readonly string _cacheFilePath;
    private readonly ILogger<LearningCacheService> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public LearningCacheService(IWebHostEnvironment environment, ILogger<LearningCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheFilePath = Path.Combine(environment.ContentRootPath, "learning-cache.json");

        LoadCacheFromFile();
    }

    public int GetFrequencyCount(int tagId)
    {
        return _cache.TryGetValue(tagId, out var count) ? count : 0;
    }

    public double GetBonusWeight(int tagId)
    {
        int count = GetFrequencyCount(tagId);
        if (count <= 0) return 0.0;

        // Mỗi lần được chọn thưởng thêm +0.02, tối đa không quá +0.25 (25% bonus)
        return Math.Min(0.25, count * 0.02);
    }

    public async Task RecordFeedbackAsync(IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        if (tagIds == null) return;

        bool updated = false;
        foreach (var tagId in tagIds)
        {
            if (tagId > 0)
            {
                _cache.AddOrUpdate(tagId, 1, (_, current) => current + 1);
                updated = true;
            }
        }

        if (updated)
        {
            await SaveCacheToFileAsync(cancellationToken);
        }
    }

    public Dictionary<int, int> GetAllCounts()
    {
        return new Dictionary<int, int>(_cache);
    }

    private void LoadCacheFromFile()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<int, int>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _cache[kvp.Key] = kvp.Value;
                    }
                    _logger.LogInformation("Loaded {Count} tag entries from Learning Cache file", _cache.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Learning Cache from file: {Path}", _cacheFilePath);
        }
    }

    private async Task SaveCacheToFileAsync(CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var snapshot = new Dictionary<int, int>(_cache);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_cacheFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist Learning Cache to file: {Path}", _cacheFilePath);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
