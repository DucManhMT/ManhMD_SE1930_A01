using System.Collections.Concurrent;
using FUNewsManagement_AIAPI.Controllers;
using FUNewsManagement_AIAPI.Models;
using FUNewsManagement_AIAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FUNews.Tests;

public class AiApiTests
{
    private class MockTagCatalogClient : ITagCatalogClient
    {
        public List<CoreTagDto> Tags { get; set; } = new();

        public Task<List<CoreTagDto>> GetAvailableTagsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Tags);
    }

    private class MockLearningCacheService : ILearningCacheService
    {
        private readonly ConcurrentDictionary<int, int> _counts = new();

        public int GetFrequencyCount(int tagId) => _counts.TryGetValue(tagId, out var c) ? c : 0;

        public double GetBonusWeight(int tagId)
        {
            int c = GetFrequencyCount(tagId);
            return Math.Min(0.25, c * 0.05);
        }

        public Task RecordFeedbackAsync(IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
        {
            foreach (var id in tagIds)
            {
                _counts.AddOrUpdate(id, 1, (_, current) => current + 1);
            }
            return Task.CompletedTask;
        }

        public Dictionary<int, int> GetAllCounts() => new(_counts);
    }

    private static IConfiguration CreateDefaultConfiguration()
    {
        var dict = new Dictionary<string, string?>
        {
            ["AiSettings:UseOpenAI"] = "false",
            ["AiSettings:OpenAIApiKey"] = ""
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task T15_SuggestTags_IdentifiesRelevantTagsAndCalculatesConfidence()
    {
        // Arrange
        var catalog = new MockTagCatalogClient
        {
            Tags = new List<CoreTagDto>
            {
                new() { TagId = 1, TagName = "Trí tuệ nhân tạo" },
                new() { TagId = 2, TagName = "Machine Learning" },
                new() { TagId = 3, TagName = "Blockchain" },
                new() { TagId = 4, TagName = "Bóng đá" }
            }
        };

        var learningCache = new MockLearningCacheService();
        var extractor = new TagExtractorService(catalog, learningCache, CreateDefaultConfiguration(), NullLogger<TagExtractorService>.Instance);

        var articleContent = "Nghiên cứu đột phá về trí tuệ nhân tạo và các mô hình machine learning đang dẫn dắt cuộc cách mạng công nghệ hiện đại.";

        // Act
        var suggestions = await extractor.SuggestTagsAsync(articleContent);

        // Assert
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Count >= 2);
        Assert.Contains(suggestions, s => s.TagId == 1 && s.TagName == "Trí tuệ nhân tạo");
        Assert.Contains(suggestions, s => s.TagId == 2 && s.TagName == "Machine Learning");
        Assert.DoesNotContain(suggestions, s => s.TagId == 3); // Blockchain
        Assert.DoesNotContain(suggestions, s => s.TagId == 4); // Bóng đá

        // Confidence nằm trong khoảng 0.0 - 1.0
        foreach (var s in suggestions)
        {
            Assert.InRange(s.Confidence, 0.1, 1.0);
        }
    }

    [Fact]
    public async Task T16_TagFeedback_IncrementsLearningCacheWeight()
    {
        // Arrange
        var learningCache = new MockLearningCacheService();

        // Act
        await learningCache.RecordFeedbackAsync(new[] { 10 });
        await learningCache.RecordFeedbackAsync(new[] { 10 });

        // Assert
        Assert.Equal(2, learningCache.GetFrequencyCount(10));
        Assert.True(learningCache.GetBonusWeight(10) > 0.0);
    }

    [Fact]
    public async Task T16_LearningCache_BoostsTagRankingInNextSuggestions()
    {
        // Arrange
        var catalog = new MockTagCatalogClient
        {
            Tags = new List<CoreTagDto>
            {
                new() { TagId = 1, TagName = "Python" },
                new() { TagId = 2, TagName = "Golang" }
            }
        };

        var learningCache = new MockLearningCacheService();
        var extractor = new TagExtractorService(catalog, learningCache, CreateDefaultConfiguration(), NullLogger<TagExtractorService>.Instance);

        var content = "Lập trình viên sử dụng python và golang để xây dựng hệ thống phân tán.";

        // Ban đầu chưa có feedback, cả hai tag có cùng tần suất
        var initial = await extractor.SuggestTagsAsync(content);
        var initialPython = initial.First(s => s.TagId == 1);
        var initialGolang = initial.First(s => s.TagId == 2);
        Assert.Equal(initialPython.Confidence, initialGolang.Confidence);

        // Act: Người dùng chọn Python nhiều lần qua Feedback Loop
        await learningCache.RecordFeedbackAsync(new[] { 1, 1, 1 });

        // Lấy lại gợi ý lần 2
        var updated = await extractor.SuggestTagsAsync(content);
        var boostedPython = updated.First(s => s.TagId == 1);
        var normalGolang = updated.First(s => s.TagId == 2);

        // Assert: Python được cộng điểm thưởng và xếp trên Golang
        Assert.True(boostedPython.Confidence > normalGolang.Confidence);
        Assert.Equal(1, updated[0].TagId); // Python lên top 1
    }

    [Fact]
    public async Task AiTagController_SuggestTags_ReturnsOkResult()
    {
        // Arrange
        var catalog = new MockTagCatalogClient
        {
            Tags = new List<CoreTagDto>
            {
                new() { TagId = 1, TagName = "Cloud" }
            }
        };
        var learningCache = new MockLearningCacheService();
        var extractor = new TagExtractorService(catalog, learningCache, CreateDefaultConfiguration(), NullLogger<TagExtractorService>.Instance);
        var controller = new AiTagController(extractor, learningCache);

        // Act
        var result = await controller.SuggestTags(new AiTagRequestDto { Content = "Bài viết về điện toán đám mây Cloud hiện đại" }, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TagSuggestionResponseDto>(okResult.Value);
        Assert.Single(response.SuggestedTags);
        Assert.Equal("Cloud", response.SuggestedTags[0].TagName);
        Assert.Single(response.Tags); // Kiểm tra alias 'tags'
    }

    [Fact]
    public async Task AiTagController_SuggestTags_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var catalog = new MockTagCatalogClient();
        var learningCache = new MockLearningCacheService();
        var extractor = new TagExtractorService(catalog, learningCache, CreateDefaultConfiguration(), NullLogger<TagExtractorService>.Instance);
        var controller = new AiTagController(extractor, learningCache);

        // Act
        var result = await controller.SuggestTags(new AiTagRequestDto { Content = "   " }, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task AiTagController_TagFeedback_ValidFeedback_ReturnsOkResult()
    {
        // Arrange
        var catalog = new MockTagCatalogClient();
        var learningCache = new MockLearningCacheService();
        var extractor = new TagExtractorService(catalog, learningCache, CreateDefaultConfiguration(), NullLogger<TagExtractorService>.Instance);
        var controller = new AiTagController(extractor, learningCache);

        // Act
        var result = await controller.TagFeedback(new TagFeedbackRequestDto { TagId = 5, ArticleId = "ART_01" }, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TagFeedbackResponseDto>(okResult.Value);
        Assert.Contains(5, response.RecordedTagIds);
        Assert.Equal(1, learningCache.GetFrequencyCount(5));
    }

    [Fact]
    public async Task AiTagController_TagFeedback_MissingTagId_ReturnsBadRequest()
    {
        // Arrange
        var catalog = new MockTagCatalogClient();
        var learningCache = new MockLearningCacheService();
        var extractor = new TagExtractorService(catalog, learningCache, CreateDefaultConfiguration(), NullLogger<TagExtractorService>.Instance);
        var controller = new AiTagController(extractor, learningCache);

        // Act
        var result = await controller.TagFeedback(new TagFeedbackRequestDto { SelectedTagIds = new List<int>() }, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
    }
}
