using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Models;
using FUNewsManagement_FE.Hubs;
using ManhMD_SE1930_A01_FE.Pages.Staff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class SignalRNotificationTests
{
    private class FakeNotificationStore : INotificationStore
    {
        public List<ArticleNotificationModel> Added { get; } = new();

        public void AddNotification(ArticleNotificationModel notification)
        {
            Added.Add(notification);
        }

        public IReadOnlyList<ArticleNotificationModel> GetRecentNotifications(int maxCount = 10)
        {
            return Added.OrderByDescending(n => n.CreatedDate).Take(maxCount).ToList();
        }
    }

    private class FakeRealtimeNotificationService : IRealtimeNotificationService
    {
        public List<ArticleNotificationModel> Broadcasted { get; } = new();

        public Task BroadcastNewArticleAsync(ArticleNotificationModel notification, CancellationToken cancellationToken = default)
        {
            Broadcasted.Add(notification);
            return Task.CompletedTask;
        }

        public IReadOnlyList<ArticleNotificationModel> GetRecentNotifications(int maxCount = 10)
        {
            return Broadcasted.OrderByDescending(n => n.CreatedDate).Take(maxCount).ToList();
        }
    }

    private class FakeNewsClientService : INewsClientService
    {
        public List<NewsArticleApiModel> Articles { get; set; } = new();

        public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ODataEnvelope<NewsArticleApiModel> { Count = Articles.Count, Value = Articles });

        public Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Articles.FirstOrDefault(a => a.NewsArticleId == id));

        public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default)
        {
            var created = new NewsArticleApiModel
            {
                NewsArticleId = $"N{Articles.Count + 1}",
                NewsTitle = request.NewsTitle,
                Headline = request.Headline,
                NewsContent = request.NewsContent,
                NewsSource = request.NewsSource,
                CategoryId = request.CategoryId,
                CategoryName = "Công nghệ",
                AuthorName = "Alice Staff",
                NewsStatus = request.NewsStatus ?? true,
                CreatedDate = DateTime.Now
            };
            Articles.Add(created);
            return Task.FromResult(created);
        }

        public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ODataEnvelope<NewsArticleApiModel> { Count = 0, Value = new List<NewsArticleApiModel>() });

        public Task<List<NewsArticleApiModel>> GetRelatedNewsArticlesAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<NewsArticleApiModel>());

        public Task<string> UploadNewsImageAsync(string articleId, System.IO.Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
            => Task.FromResult($"/uploads/news/{fileName}");
    }

    private class FakeCategoryClientService : ICategoryClientService
    {
        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ODataEnvelope<CategoryApiModel>
            {
                Count = 1,
                Value = new List<CategoryApiModel>
                {
                    new CategoryApiModel { CategoryId = 1, CategoryName = "Công nghệ", IsActive = true }
                }
            });
        }

        public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryApiModel?>(null);
        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeTagClientService : ITagClientService
    {
        public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ODataEnvelope<TagApiModel> { Count = 0, Value = new List<TagApiModel>() });
        public Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<TagApiModel?>(null);
        public Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<NewsArticleApiModel>> GetArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default) => Task.FromResult(new List<NewsArticleApiModel>());
    }

    private class FakeAiClientService : IAiClientService
    {
        public Task<List<TagSuggestionApiModel>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<TagSuggestionApiModel>());
        public Task SubmitTagFeedbackAsync(int tagId, string? articleId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SubmitMultipleTagFeedbacksAsync(IEnumerable<int> tagIds, string? articleId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void NotificationStore_AddAndGetRecent_MaintainsLimitAndOrder()
    {
        // Arrange
        var store = new NotificationStore();

        // Act - Add 15 notifications
        for (int i = 1; i <= 15; i++)
        {
            store.AddNotification(new ArticleNotificationModel
            {
                ArticleId = $"ART{i:D3}",
                Title = $"News Title {i}",
                Author = "Staff User",
                CategoryName = "General",
                CreatedDate = DateTime.UtcNow.AddMinutes(i)
            });
        }

        var recent = store.GetRecentNotifications(10);

        // Assert
        Assert.Equal(10, recent.Count);
        // The most recent added (ART015) should be first
        Assert.Equal("ART015", recent[0].ArticleId);
        Assert.Equal("ART006", recent[9].ArticleId);
    }

    [Fact]
    public async Task NotificationHub_GetRecentNotifications_ReturnsFromStore()
    {
        // Arrange
        var fakeStore = new FakeNotificationStore();
        fakeStore.AddNotification(new ArticleNotificationModel
        {
            ArticleId = "ART101",
            Title = "Realtime Update",
            CreatedDate = DateTime.UtcNow
        });

        var hub = new NotificationHub(fakeStore, NullLogger<NotificationHub>.Instance);

        // Act
        var result = await hub.GetRecentNotifications();

        // Assert
        Assert.Single(result);
        Assert.Equal("ART101", result[0].ArticleId);
        Assert.Equal("Realtime Update", result[0].Title);
    }

    [Fact]
    public async Task NewsModel_OnPostCreateAsync_BroadcastsNotification_WhenArticleCreated()
    {
        // Arrange
        var fakeNewsClient = new FakeNewsClientService();
        var fakeCategoryClient = new FakeCategoryClientService();
        var fakeTagClient = new FakeTagClientService();
        var fakeAiClient = new FakeAiClientService();
        var fakeNotificationService = new FakeRealtimeNotificationService();

        var pageModel = new NewsModel(
            fakeNewsClient,
            fakeCategoryClient,
            fakeTagClient,
            fakeAiClient,
            NullLogger<NewsModel>.Instance,
            fakeNotificationService);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Alice Staff"),
            new Claim(ClaimTypes.Role, "Staff")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);

        pageModel.PageContext = new PageContext(actionContext);

        var input = new CreateNewsArticleInputModel
        {
            NewsTitle = "New AI Breakthrough",
            Headline = "Major advancement in generative AI",
            NewsContent = "Detailed content here...",
            CategoryId = 1,
            NewsStatus = true
        };

        // Act
        var result = await pageModel.OnPostCreateAsync(input, CancellationToken.None);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
        Assert.Single(fakeNotificationService.Broadcasted);
        var broadcasted = fakeNotificationService.Broadcasted[0];
        Assert.Equal("N1", broadcasted.ArticleId);
        Assert.Equal("New AI Breakthrough", broadcasted.Title);
        Assert.Equal("Công nghệ", broadcasted.CategoryName);
        Assert.Equal("Alice Staff", broadcasted.Author);
    }

    private class SessionTestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
