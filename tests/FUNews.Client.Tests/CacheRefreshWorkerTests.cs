using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using FUNews.Client.DataAccess.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class CacheRefreshWorkerTests
{
    private class FakeCoreApiClientForWorker : ICoreApiClient
    {
        public bool ShouldThrow { get; set; }

        public List<CategoryApiModel> CategoriesToReturn { get; set; } = new()
        {
            new CategoryApiModel { CategoryId = 1, CategoryName = "Tin Công Nghệ", CategoryDescription = "Mô tả", IsActive = true }
        };

        public List<TagApiModel> TagsToReturn { get; set; } = new()
        {
            new TagApiModel { TagId = 1, TagName = "AI", Note = "Trí tuệ nhân tạo" }
        };

        public List<NewsArticleApiModel> ArticlesToReturn { get; set; } = new()
        {
            new NewsArticleApiModel { NewsArticleId = "N01", NewsTitle = "Tin mới 2026", Headline = "HL", NewsStatus = true }
        };

        public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldThrow) throw new FUNewsApiException(System.Net.HttpStatusCode.ServiceUnavailable, "Core API Unavailable");
            return Task.FromResult(new ODataEnvelope<CategoryApiModel> { Count = CategoriesToReturn.Count, Value = CategoriesToReturn });
        }

        public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldThrow) throw new FUNewsApiException(System.Net.HttpStatusCode.ServiceUnavailable, "Core API Unavailable");
            return Task.FromResult(new ODataEnvelope<TagApiModel> { Count = TagsToReturn.Count, Value = TagsToReturn });
        }

        public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
        {
            if (ShouldThrow) throw new FUNewsApiException(System.Net.HttpStatusCode.ServiceUnavailable, "Core API Unavailable");
            return Task.FromResult(new ODataEnvelope<NewsArticleApiModel> { Count = ArticlesToReturn.Count, Value = ArticlesToReturn });
        }

        public Task<LoginResponseApiModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel?> GetCategoryByIdAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TagApiModel?> GetTagByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<NewsArticleApiModel>> GetNewsArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<NewsArticleApiModel>> GetRelatedNewsArticlesAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> UploadNewsImageAsync(string articleId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ODataEnvelope<AccountApiModel>> GetAccountsAsync(string? odataQuery = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel?> GetAccountByIdAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> CreateAccountAsync(CreateAccountApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> UpdateAccountAsync(short id, UpdateAccountApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAccountAsync(short id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> GetProfileAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AccountApiModel> UpdateProfileAsync(UpdateProfileApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ChangePasswordAsync(ChangePasswordApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LoginResponseApiModel> LoginAsync(LoginRequestApiModel request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<NewsReportApiModel> GetReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task OfflineStoreManager_SaveAndGetSnapshot_Succeeds()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"snapshot_test_{Guid.NewGuid():N}.json");
        try
        {
            var storeManager = new OfflineStoreManager(tempFile, NullLogger<OfflineStoreManager>.Instance);

            Assert.False(storeManager.HasSnapshot());

            var snapshot = new OfflineDataSnapshot
            {
                RefreshedAt = DateTime.UtcNow,
                Categories = new List<CategoryApiModel>
                {
                    new() { CategoryId = 1, CategoryName = "Thể thao" }
                },
                Tags = new List<TagApiModel>
                {
                    new() { TagId = 10, TagName = "Bóng đá" }
                },
                Articles = new List<NewsArticleApiModel>
                {
                    new() { NewsArticleId = "ART_01", NewsTitle = "Trận đấu bóng đá hôm nay", NewsStatus = true }
                }
            };

            await storeManager.SaveSnapshotAsync(snapshot);

            Assert.True(storeManager.HasSnapshot());
            Assert.True(File.Exists(tempFile));

            var loadedSnapshot = await storeManager.GetSnapshotAsync();
            Assert.NotNull(loadedSnapshot);
            Assert.Single(loadedSnapshot.Categories);
            Assert.Equal("Thể thao", loadedSnapshot.Categories[0].CategoryName);
            Assert.Single(loadedSnapshot.Tags);
            Assert.Equal("Bóng đá", loadedSnapshot.Tags[0].TagName);
            Assert.Single(loadedSnapshot.Articles);
            Assert.Equal("ART_01", loadedSnapshot.Articles[0].NewsArticleId);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CacheRefreshWorker_RefreshCacheAsync_FetchesDataAndSavesSnapshot()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"worker_snapshot_test_{Guid.NewGuid():N}.json");
        try
        {
            var storeManager = new OfflineStoreManager(tempFile, NullLogger<OfflineStoreManager>.Instance);

            var services = new ServiceCollection();
            var fakeCore = new FakeCoreApiClientForWorker();
            services.AddSingleton<ICoreApiClient>(fakeCore);
            var serviceProvider = services.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CacheRefreshWorker:IntervalHours", "6" },
                { "CacheRefreshWorker:InitialDelaySeconds", "0" }
            }).Build();

            var worker = new CacheRefreshWorker(
                scopeFactory,
                storeManager,
                config,
                NullLogger<CacheRefreshWorker>.Instance);

            var success = await worker.RefreshCacheAsync();

            Assert.True(success);
            Assert.True(storeManager.HasSnapshot());

            var cached = storeManager.GetCachedSnapshot();
            Assert.NotNull(cached);
            Assert.Single(cached.Categories);
            Assert.Equal(1, cached.Categories[0].CategoryId);
            Assert.Single(cached.Tags);
            Assert.Equal("AI", cached.Tags[0].TagName);
            Assert.Single(cached.Articles);
            Assert.Equal("N01", cached.Articles[0].NewsArticleId);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CacheRefreshWorker_WhenApiFails_HandlesGracefullyWithoutThrowing()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"worker_fail_test_{Guid.NewGuid():N}.json");
        try
        {
            var storeManager = new OfflineStoreManager(tempFile, NullLogger<OfflineStoreManager>.Instance);

            var services = new ServiceCollection();
            var fakeCore = new FakeCoreApiClientForWorker { ShouldThrow = true };
            services.AddSingleton<ICoreApiClient>(fakeCore);
            var serviceProvider = services.BuildServiceProvider();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CacheRefreshWorker:IntervalMinutes", "360" }
            }).Build();

            var worker = new CacheRefreshWorker(
                scopeFactory,
                storeManager,
                config,
                NullLogger<CacheRefreshWorker>.Instance);

            // Không được văng ngoại lệ dù Core API bị lỗi ServiceUnavailable
            var success = await worker.RefreshCacheAsync();

            Assert.False(success);
            Assert.False(storeManager.HasSnapshot());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
