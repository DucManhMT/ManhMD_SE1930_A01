using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FUNewsManagement_FE.Hubs;

public class ArticleNotificationModel
{
    public string ArticleId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public interface INotificationStore
{
    void AddNotification(ArticleNotificationModel notification);
    IReadOnlyList<ArticleNotificationModel> GetRecentNotifications(int maxCount = 10);
}

public class NotificationStore : INotificationStore
{
    private readonly ConcurrentQueue<ArticleNotificationModel> _queue = new();
    private const int MaxStoreCapacity = 50;

    public void AddNotification(ArticleNotificationModel notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _queue.Enqueue(notification);

        // Giữ kích thước vừa phải, không để phình bộ nhớ
        while (_queue.Count > MaxStoreCapacity && _queue.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<ArticleNotificationModel> GetRecentNotifications(int maxCount = 10)
    {
        return _queue
            .OrderByDescending(n => n.CreatedDate)
            .Take(maxCount)
            .ToList();
    }
}

public class NotificationHub : Hub
{
    private readonly INotificationStore _store;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(INotificationStore store, ILogger<NotificationHub> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<ArticleNotificationModel>> GetRecentNotifications()
    {
        var recent = _store.GetRecentNotifications(10);
        return Task.FromResult(recent);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

public interface IRealtimeNotificationService
{
    Task BroadcastNewArticleAsync(ArticleNotificationModel notification, CancellationToken cancellationToken = default);
    IReadOnlyList<ArticleNotificationModel> GetRecentNotifications(int maxCount = 10);
}

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationStore _store;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<NotificationHub> hubContext,
        INotificationStore store,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task BroadcastNewArticleAsync(ArticleNotificationModel notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _store.AddNotification(notification);

        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("Broadcasted article notification: #{Id} - {Title}", notification.ArticleId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast SignalR notification for article #{Id}", notification.ArticleId);
        }
    }

    public IReadOnlyList<ArticleNotificationModel> GetRecentNotifications(int maxCount = 10)
    {
        return _store.GetRecentNotifications(maxCount);
    }
}
