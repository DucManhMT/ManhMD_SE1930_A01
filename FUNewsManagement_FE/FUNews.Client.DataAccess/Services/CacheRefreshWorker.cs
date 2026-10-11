using System;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FUNews.Client.DataAccess.Services;

public class CacheRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOfflineStoreManager _storeManager;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<CacheRefreshWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _initialDelay;

    public CacheRefreshWorker(
        IServiceScopeFactory scopeFactory,
        IOfflineStoreManager storeManager,
        IConfiguration? configuration,
        ILogger<CacheRefreshWorker> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _storeManager = storeManager ?? throw new ArgumentNullException(nameof(storeManager));
        _configuration = configuration;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Chu kỳ mặc định 6 giờ theo yêu cầu đề bài
        _interval = ResolveInterval();
        _initialDelay = ResolveInitialDelay();

        _logger.LogInformation("CacheRefreshWorker initialized with interval: {Interval}, initial delay: {InitialDelay}",
            _interval, _initialDelay);
    }

    private TimeSpan ResolveInterval()
    {
        if (_configuration != null)
        {
            if (int.TryParse(_configuration["CacheRefreshWorker:IntervalSeconds"], out var sec) && sec > 0)
            {
                return TimeSpan.FromSeconds(sec);
            }
            if (int.TryParse(_configuration["CacheRefreshWorker:IntervalMinutes"], out var min) && min > 0)
            {
                return TimeSpan.FromMinutes(min);
            }
            if (int.TryParse(_configuration["CacheRefreshWorker:IntervalHours"], out var hours) && hours > 0)
            {
                return TimeSpan.FromHours(hours);
            }
        }
        return TimeSpan.FromHours(6);
    }

    private TimeSpan ResolveInitialDelay()
    {
        if (_configuration != null && int.TryParse(_configuration["CacheRefreshWorker:InitialDelaySeconds"], out var delaySec))
        {
            return TimeSpan.FromSeconds(Math.Max(0, delaySec));
        }
        return TimeSpan.FromSeconds(3); // Delay nhẹ khi ứng dụng khởi động
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CacheRefreshWorker background service is starting.");

        if (_initialDelay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(_initialDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CacheRefreshWorker cancelled during initial delay.");
                return;
            }
        }

        // Chạy lần đầu tiên ngay khi worker khởi động
        await RefreshCacheAsync(stoppingToken);

        // Chu kỳ lặp định kỳ (PeriodicTimer)
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Periodic timer fired for CacheRefreshWorker at {Time}", DateTime.UtcNow);
                await RefreshCacheAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CacheRefreshWorker execution stopped via cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CacheRefreshWorker background loop.");
        }
    }

    public async Task<bool> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled cache refresh from Core API...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var coreClient = scope.ServiceProvider.GetRequiredService<ICoreApiClient>();

            // Lấy danh mục, thẻ tin và các bài viết Active
            var catTask = coreClient.GetCategoriesAsync(cancellationToken: cancellationToken);
            var tagTask = coreClient.GetTagsAsync(cancellationToken: cancellationToken);
            var newsTask = coreClient.GetNewsArticlesAsync(
                odataQuery: "$filter=newsStatus eq true&$orderby=createdDate desc",
                cancellationToken: cancellationToken);

            await Task.WhenAll(catTask, tagTask, newsTask);

            var categories = (await catTask)?.Value ?? new();
            var tags = (await tagTask)?.Value ?? new();
            var articles = (await newsTask)?.Value ?? new();

            var snapshot = new OfflineDataSnapshot
            {
                RefreshedAt = DateTime.UtcNow,
                Categories = categories,
                Tags = tags,
                Articles = articles
            };

            await _storeManager.SaveSnapshotAsync(snapshot, cancellationToken);

            _logger.LogInformation("Cache refresh completed successfully: {CatCount} categories, {TagCount} tags, {ArtCount} articles snapshot saved.",
                categories.Count, tags.Count, articles.Count);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Cache refresh was cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            // Bắt ngoại lệ và ghi log, tuyệt đối không làm crash ứng dụng nếu Core API tạm thời không khả dụng
            _logger.LogWarning(ex, "Failed to refresh offline cache from Core API. Will retry on next scheduled interval.");
            return false;
        }
    }
}
