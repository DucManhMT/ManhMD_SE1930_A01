using System.Text;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Services;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Repositories;
using Xunit;

namespace FUNews.Tests;

public class MediaUploadTests
{
    private static readonly byte[] ValidPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    private static readonly byte[] ValidJpgBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
    private static readonly byte[] ValidWebpBytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
    private static readonly byte[] FakeExeBytes = Encoding.UTF8.GetBytes("MZ_FAKE_EXECUTABLE_CONTENT");

    private class FakeNewsArticleRepository : INewsArticleRepository
    {
        public List<NewsArticle> Articles { get; set; } = new();

        public IQueryable<NewsArticle> GetQueryable() => Articles.AsQueryable();

        public Task<NewsArticle?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Articles.FirstOrDefault(a => a.NewsArticleID == id));

        public Task<NewsArticle?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Articles.FirstOrDefault(a => a.NewsArticleID == id));

        public Task<List<NewsArticle>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Articles.ToList());

        public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Articles.Any(a => a.NewsArticleID == id));

        public Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken = default)
        {
            Articles.Add(article);
            return Task.FromResult(article);
        }

        public Task<NewsArticle> CreateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
        {
            Articles.Add(article);
            return Task.FromResult(article);
        }

        public Task<NewsArticle> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default)
        {
            var idx = Articles.FindIndex(a => a.NewsArticleID == article.NewsArticleID);
            if (idx >= 0)
            {
                Articles[idx] = article;
            }
            return Task.FromResult(article);
        }

        public Task<NewsArticle> UpdateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
        {
            var idx = Articles.FindIndex(a => a.NewsArticleID == article.NewsArticleID);
            if (idx >= 0)
            {
                Articles[idx] = article;
            }
            return Task.FromResult(article);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var count = Articles.RemoveAll(a => a.NewsArticleID == id);
            return Task.FromResult(count > 0);
        }
    }

    [Fact]
    public async Task UploadNewsImageAsync_ValidPngImage_SavesFileAndUpdatesNewsArticle()
    {
        // Arrange
        var articleId = "TEST_IMG_01";
        var article = new NewsArticle
        {
            NewsArticleID = articleId,
            Headline = "Test Image Upload",
            NewsStatus = true
        };

        var repo = new FakeNewsArticleRepository
        {
            Articles = new List<NewsArticle> { article }
        };

        var mediaService = new MediaService(repo);
        using var stream = new MemoryStream(ValidPngBytes);

        // Act
        var resultUrl = await mediaService.UploadNewsImageAsync(
            articleId,
            stream,
            "sample-image.png",
            "image/png",
            ValidPngBytes.Length);

        // Assert
        Assert.NotNull(resultUrl);
        Assert.StartsWith("/uploads/news/", resultUrl);
        Assert.EndsWith(".png", resultUrl);
        Assert.Equal(resultUrl, article.NewsImage);
    }

    [Fact]
    public async Task UploadNewsImageAsync_ValidJpegImage_SavesFileAndUpdatesNewsArticle()
    {
        // Arrange
        var articleId = "TEST_IMG_02";
        var article = new NewsArticle
        {
            NewsArticleID = articleId,
            Headline = "Test JPEG Upload",
            NewsStatus = true
        };

        var repo = new FakeNewsArticleRepository
        {
            Articles = new List<NewsArticle> { article }
        };

        var mediaService = new MediaService(repo);
        using var stream = new MemoryStream(ValidJpgBytes);

        // Act
        var resultUrl = await mediaService.UploadNewsImageAsync(
            articleId,
            stream,
            "banner.jpeg",
            "image/jpeg",
            ValidJpgBytes.Length);

        // Assert
        Assert.NotNull(resultUrl);
        Assert.StartsWith("/uploads/news/", resultUrl);
        Assert.EndsWith(".jpeg", resultUrl);
        Assert.Equal(resultUrl, article.NewsImage);
    }

    [Fact]
    public async Task UploadNewsImageAsync_FileExceeds5MB_ThrowsPayloadTooLargeException()
    {
        // Arrange (5MB + 1 byte)
        var articleId = "TEST_IMG_03";
        var repo = new FakeNewsArticleRepository();
        var mediaService = new MediaService(repo);

        using var stream = new MemoryStream(ValidPngBytes);
        var oversizedLength = (5 * 1024 * 1024) + 1;

        // Act & Assert (HTTP 413)
        var ex = await Assert.ThrowsAsync<PayloadTooLargeException>(() =>
            mediaService.UploadNewsImageAsync(
                articleId,
                stream,
                "giant_image.png",
                "image/png",
                oversizedLength));

        Assert.Contains("vượt quá giới hạn cho phép (tối đa 5MB)", ex.Message);
    }

    [Fact]
    public async Task UploadNewsImageAsync_InvalidExtension_ThrowsUnsupportedMediaTypeException()
    {
        // Arrange (.exe file extension)
        var articleId = "TEST_IMG_04";
        var repo = new FakeNewsArticleRepository();
        var mediaService = new MediaService(repo);

        using var stream = new MemoryStream(FakeExeBytes);

        // Act & Assert (HTTP 415)
        var ex = await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() =>
            mediaService.UploadNewsImageAsync(
                articleId,
                stream,
                "virus_payload.exe",
                "application/x-msdownload",
                FakeExeBytes.Length));

        Assert.Contains("Định dạng file không được hỗ trợ", ex.Message);
    }

    [Fact]
    public async Task UploadNewsImageAsync_FakeExtensionWithInvalidMagicBytes_ThrowsUnsupportedMediaTypeException()
    {
        // Arrange: named .png but header is plain text / exe bytes
        var articleId = "TEST_IMG_05";
        var repo = new FakeNewsArticleRepository();
        var mediaService = new MediaService(repo);

        using var stream = new MemoryStream(FakeExeBytes);

        // Act & Assert (HTTP 415)
        var ex = await Assert.ThrowsAsync<UnsupportedMediaTypeException>(() =>
            mediaService.UploadNewsImageAsync(
                articleId,
                stream,
                "fake_image.png",
                "image/png",
                FakeExeBytes.Length));

        Assert.Contains("Nội dung file không khớp với định dạng ảnh hợp lệ", ex.Message);
    }

    [Fact]
    public async Task UploadNewsImageAsync_NonExistentArticle_ThrowsNotFoundException()
    {
        // Arrange
        var articleId = "NOT_FOUND_99";
        var repo = new FakeNewsArticleRepository();
        var mediaService = new MediaService(repo);
        using var stream = new MemoryStream(ValidPngBytes);

        // Act & Assert (HTTP 404)
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            mediaService.UploadNewsImageAsync(
                articleId,
                stream,
                "photo.png",
                "image/png",
                ValidPngBytes.Length));

        Assert.Contains("không tồn tại", ex.Message);
    }

    [Fact]
    public async Task UploadNewsImageAsync_ZeroLengthFile_ThrowsValidationException()
    {
        // Arrange
        var articleId = "TEST_IMG_06";
        var repo = new FakeNewsArticleRepository();
        var mediaService = new MediaService(repo);
        using var stream = new MemoryStream();

        // Act & Assert (HTTP 400)
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            mediaService.UploadNewsImageAsync(
                articleId,
                stream,
                "empty.png",
                "image/png",
                0));

        Assert.True(ex.Errors.ContainsKey("File"));
        Assert.Contains("Vui lòng chọn file", ex.Errors["File"][0]);
    }
}
