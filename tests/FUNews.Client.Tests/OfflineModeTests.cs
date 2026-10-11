using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using FUNews.Client.DataAccess.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FUNews.Client.Tests;

public class OfflineModeTests
{
    private class FakeFailingHttpMessageHandler : HttpMessageHandler
    {
        public bool FailWithException { get; set; } = true;
        public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.ServiceUnavailable;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (FailWithException)
            {
                throw new HttpRequestException("Core API is unreachable (Offline)");
            }

            return Task.FromResult(new HttpResponseMessage(StatusCodeToReturn)
            {
                Content = new StringContent("{\"title\":\"Service Unavailable\",\"status\":503}")
            });
        }
    }

    private class FakeSuccessHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"count\":1,\"value\":[{\"categoryId\":1,\"categoryName\":\"Công nghệ\"}]}")
            });
        }
    }

    [Fact]
    public async Task CoreApiClient_WhenApiFails_FallbacksToOfflineStoreAndMarksOffline()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"offline_test_{Guid.NewGuid():N}.json");
        try
        {
            var storeManager = new OfflineStoreManager(tempFile, NullLogger<OfflineStoreManager>.Instance);

            // Chuẩn bị sẵn dữ liệu trong snapshot
            var snapshot = new OfflineDataSnapshot
            {
                RefreshedAt = DateTime.UtcNow,
                Categories = new List<CategoryApiModel>
                {
                    new() { CategoryId = 1, CategoryName = "Chuyên mục Cache" }
                },
                Tags = new List<TagApiModel>
                {
                    new() { TagId = 2, TagName = "Tag Cache" }
                },
                Articles = new List<NewsArticleApiModel>
                {
                    new() { NewsArticleId = "N_OFFLINE_01", NewsTitle = "Tin tức từ bộ nhớ đệm", NewsStatus = true }
                }
            };
            await storeManager.SaveSnapshotAsync(snapshot);

            Assert.False(storeManager.IsOffline);

            // Giả lập HttpClient gặp lỗi kết nối
            var failingHandler = new FakeFailingHttpMessageHandler();
            var httpClient = new HttpClient(failingHandler)
            {
                BaseAddress = new Uri("https://localhost:7001/")
            };

            var coreClient = new CoreApiClient(httpClient, storeManager);

            // 1. Thử lấy danh mục khi mất mạng -> Phải đọc từ snapshot và chuyển sang Offline Mode
            var categories = await coreClient.GetCategoriesAsync();
            Assert.NotNull(categories);
            Assert.Single(categories.Value);
            Assert.Equal("Chuyên mục Cache", categories.Value[0].CategoryName);
            Assert.True(storeManager.IsOffline, "Hệ thống phải tự động chuyển sang IsOffline = true");

            // 2. Thử lấy thẻ tin khi mất mạng -> Phải đọc từ snapshot
            var tags = await coreClient.GetTagsAsync();
            Assert.NotNull(tags);
            Assert.Single(tags.Value);
            Assert.Equal("Tag Cache", tags.Value[0].TagName);

            // 3. Thử lấy bài viết khi mất mạng -> Phải đọc từ snapshot
            var articles = await coreClient.GetNewsArticlesAsync();
            Assert.NotNull(articles);
            Assert.Single(articles.Value);
            Assert.Equal("N_OFFLINE_01", articles.Value[0].NewsArticleId);

            // 4. Thử lấy bài viết theo Id khi mất mạng
            var article = await coreClient.GetNewsArticleByIdAsync("N_OFFLINE_01");
            Assert.NotNull(article);
            Assert.Equal("Tin tức từ bộ nhớ đệm", article.NewsTitle);
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
    public async Task CoreApiClient_WhenOffline_BlocksCrudMutations()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"offline_crud_test_{Guid.NewGuid():N}.json");
        try
        {
            var storeManager = new OfflineStoreManager(tempFile, NullLogger<OfflineStoreManager>.Instance);
            storeManager.IsOffline = true; // Hệ thống đang trong trạng thái ngoại tuyến

            var httpClient = new HttpClient(new FakeFailingHttpMessageHandler())
            {
                BaseAddress = new Uri("https://localhost:7001/")
            };
            var coreClient = new CoreApiClient(httpClient, storeManager);

            // Thử tạo bài viết -> Phải bị chặn với ServiceUnavailable
            var exCreate = await Assert.ThrowsAsync<FUNewsApiException>(() =>
                coreClient.CreateNewsArticleAsync(new CreateNewsArticleApiModel { Headline = "HL" }));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, exCreate.StatusCode);
            Assert.Contains("ngoại tuyến", exCreate.Message);

            // Thử cập nhật bài viết -> Phải bị chặn
            var exUpdate = await Assert.ThrowsAsync<FUNewsApiException>(() =>
                coreClient.UpdateNewsArticleAsync("N01", new UpdateNewsArticleApiModel { Headline = "HL" }));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, exUpdate.StatusCode);
            Assert.Contains("ngoại tuyến", exUpdate.Message);

            // Thử xóa bài viết -> Phải bị chặn
            var exDelete = await Assert.ThrowsAsync<FUNewsApiException>(() =>
                coreClient.DeleteNewsArticleAsync("N01"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, exDelete.StatusCode);
            Assert.Contains("ngoại tuyến", exDelete.Message);

            // Thử nhân bản bài viết -> Phải bị chặn
            var exDup = await Assert.ThrowsAsync<FUNewsApiException>(() =>
                coreClient.DuplicateNewsArticleAsync("N01"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, exDup.StatusCode);
            Assert.Contains("ngoại tuyến", exDup.Message);
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
    public async Task CoreApiClient_WhenApiRestored_MarksOnlineAutomatically()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"online_restore_test_{Guid.NewGuid():N}.json");
        try
        {
            var storeManager = new OfflineStoreManager(tempFile, NullLogger<OfflineStoreManager>.Instance);
            storeManager.IsOffline = true; // Trước đó bị offline

            var successHandler = new FakeSuccessHttpMessageHandler();
            var httpClient = new HttpClient(successHandler)
            {
                BaseAddress = new Uri("https://localhost:7001/")
            };
            var coreClient = new CoreApiClient(httpClient, storeManager);

            // Gọi API thành công -> Tự động chuyển IsOffline về false
            var categories = await coreClient.GetCategoriesAsync();
            Assert.NotNull(categories);
            Assert.False(storeManager.IsOffline, "Hệ thống phải tự động khôi phục IsOffline = false khi API kết nối lại.");
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
