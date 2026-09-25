using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using ManhMD_SE1930_A01_BE.OData;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class NewsArticleSearchTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly List<string> _testArticleIds = new();

    public NewsArticleSearchTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;

        using var scope = factory.Services.CreateScope();
        _jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    }

    public async Task InitializeAsync()
    {
        await CleanupTestArticlesAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestArticlesAsync();
    }

    private async Task CleanupTestArticlesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        if (_testArticleIds.Count > 0)
        {
            var tags = await db.NewsTags.Where(nt => _testArticleIds.Contains(nt.NewsArticleID)).ToListAsync();
            if (tags.Count > 0)
            {
                db.NewsTags.RemoveRange(tags);
            }

            var articles = await db.NewsArticles.Where(a => _testArticleIds.Contains(a.NewsArticleID)).ToListAsync();
            if (articles.Count > 0)
            {
                db.NewsArticles.RemoveRange(articles);
            }

            await db.SaveChangesAsync();
            _testArticleIds.Clear();
        }
    }

    private async Task<NewsArticle> SeedArticleAsync(
        string id,
        string title,
        string headline,
        string content,
        short categoryId,
        bool status,
        short authorId,
        DateTime createdDate,
        List<int> tagIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        var article = new NewsArticle
        {
            NewsArticleID = id,
            NewsTitle = title,
            Headline = headline,
            NewsContent = content,
            NewsSource = "FPT Education",
            CategoryID = categoryId,
            NewsStatus = status,
            CreatedByID = authorId,
            CreatedDate = createdDate
        };

        db.NewsArticles.Add(article);

        foreach (var tagId in tagIds)
        {
            db.NewsTags.Add(new NewsTag
            {
                NewsArticleID = id,
                TagID = tagId
            });
        }

        await db.SaveChangesAsync();
        _testArticleIds.Add(id);
        return article;
    }

    [Fact]
    public async Task Criterion_01_SearchCombined_FiltersCorrectly()
    {
        // AC 1: Kết hợp linh hoạt từ khóa, category, author, date range
        var article1 = await SeedArticleAsync(
            "TEST_SRCH_01",
            "Nghiên cứu Trí tuệ Nhân tạo 2026",
            "Đột phá công nghệ AI mới",
            "Nội dung chi tiết về mô hình học sâu hiện đại",
            1, // Category 1
            true, // Active
            2, // Staff author
            new DateTime(2026, 4, 15),
            new List<int> { 1, 2 }
        );

        var article2 = await SeedArticleAsync(
            "TEST_SRCH_02",
            "Hội thảo Khoa học Máy tính",
            "Diễn đàn công nghệ thông tin",
            "Nội dung hội thảo công nghệ phần mềm",
            2, // Category 2
            true,
            2,
            new DateTime(2026, 4, 20),
            new List<int> { 3 }
        );

        var client = _factory.CreateClient();

        // Tìm kiếm với category = 1 và từ khóa 'Trí tuệ'
        var url = "api/news?$filter=categoryId eq 1 and contains(newsTitle,'Trí tuệ')&$count=true";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(envelope);
        Assert.Equal(1, envelope.Count);
        Assert.Contains(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_01");
        Assert.DoesNotContain(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_02");
    }

    [Fact]
    public async Task Criterion_01_SearchByTag_UsingAnyFunction_WorksCorrectly()
    {
        // AC 1: Lọc bài viết theo Tag thông qua OData tags/any
        var article1 = await SeedArticleAsync(
            "TEST_SRCH_TAG1",
            "Học máy và Dữ liệu lớn",
            "Machine learning trong thực tế",
            "Phân tích dữ liệu lớn trên nền tảng đám mây",
            1,
            true,
            2,
            DateTime.UtcNow,
            new List<int> { 1 } // Gắn tag 1
        );

        var article2 = await SeedArticleAsync(
            "TEST_SRCH_TAG2",
            "Phát triển Ứng dụng Di động",
            "Lập trình Flutter và iOS",
            "Kỹ thuật xây dựng app di động hiện đại",
            1,
            true,
            2,
            DateTime.UtcNow,
            new List<int> { 2 } // Gắn tag 2 (không có tag 1)
        );

        var client = _factory.CreateClient();

        // OData collection any filter
        var url = "api/news?$filter=tags/any(t: t/tagId eq 1)&$count=true";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(envelope);
        Assert.Contains(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_TAG1");
        Assert.DoesNotContain(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_TAG2");
    }

    [Fact]
    public async Task Criterion_02_SearchWithSingleQuoteAndUnicode_DoesNotCrashOData()
    {
        // AC 2: Encode dấu nháy và ký tự Unicode tiếng Việt
        var article = await SeedArticleAsync(
            "TEST_SRCH_QUOTE",
            "Sách O'Reilly & Tiếng Việt có dấu: Phổ quát",
            "Tóm tắt lập trình đặc biệt",
            "Nội dung nghiên cứu chi tiết O'Reilly và khoa học",
            1,
            true,
            2,
            DateTime.UtcNow,
            new List<int>()
        );

        var client = _factory.CreateClient();

        // Escape O'Reilly thành O''Reilly trong OData filter
        var url = "api/news?$filter=contains(newsTitle,'O''Reilly') and contains(newsTitle,'Tiếng Việt')&$count=true";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(envelope);
        Assert.Contains(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_QUOTE");
    }

    [Fact]
    public async Task Criterion_04_PublicSearch_AlwaysEnforcesActiveOnly()
    {
        // AC 4: Anonymous / Lecturer tìm kiếm luôn chỉ nhận bài Active, không lộ bài Inactive
        var activeArticle = await SeedArticleAsync(
            "TEST_SRCH_PUB_ACT",
            "Bài viết công khai hoạt động",
            "Tóm tắt bài công khai",
            "Nội dung bài viết đang phát hành",
            1,
            true, // Active
            2,
            DateTime.UtcNow,
            new List<int>()
        );

        var inactiveArticle = await SeedArticleAsync(
            "TEST_SRCH_PUB_INACT",
            "Bài viết công khai tạm ẩn",
            "Tóm tắt bài tạm ẩn",
            "Nội dung bài viết chưa phát hành",
            1,
            false, // Inactive
            2,
            DateTime.UtcNow,
            new List<int>()
        );

        var client = _factory.CreateClient();

        // Anonymous client gửi request tìm kiếm
        var url = "api/news?$filter=contains(newsTitle,'Bài viết công khai')&$count=true";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ODataResponse<NewsArticleDto>>();
        Assert.NotNull(envelope);

        // Bài Active phải có trong kết quả
        Assert.Contains(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_PUB_ACT");
        // Bài Inactive TUYỆT ĐỐI không xuất hiện
        Assert.DoesNotContain(envelope.Value, a => a.NewsArticleId == "TEST_SRCH_PUB_INACT");
        // Không lộ bài Inactive trong tổng số count
        Assert.All(envelope.Value, a => Assert.True(a.NewsStatus == true));
    }

    [Fact]
    public async Task Criterion_05_MaxTop_ExceedingLimit_ReturnsBadRequest()
    {
        // AC 5: MaxTop = 100 validation
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/news?$top=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
