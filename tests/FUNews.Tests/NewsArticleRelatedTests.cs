using System.Net;
using System.Net.Http.Json;
using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FUNews.Tests;

public class NewsArticleRelatedTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<string> _testArticleIds = new();
    private readonly List<short> _testCategoryIds = new();
    private readonly List<int> _testTagIds = new();

    public NewsArticleRelatedTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
        }

        if (_testCategoryIds.Count > 0)
        {
            var cats = await db.Categories.Where(c => _testCategoryIds.Contains(c.CategoryID)).ToListAsync();
            if (cats.Count > 0)
            {
                db.Categories.RemoveRange(cats);
            }
        }

        if (_testTagIds.Count > 0)
        {
            var tgs = await db.Tags.Where(t => _testTagIds.Contains(t.TagID)).ToListAsync();
            if (tgs.Count > 0)
            {
                db.Tags.RemoveRange(tgs);
            }
        }

        await db.SaveChangesAsync();
        _testArticleIds.Clear();
        _testCategoryIds.Clear();
        _testTagIds.Clear();
    }

    private async Task<short> SeedCategoryAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        var cat = new Category
        {
            CategoryName = name,
            CategoryDescription = "Mô tả test",
            IsActive = true
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        _testCategoryIds.Add(cat.CategoryID);
        return cat.CategoryID;
    }

    private async Task<int> SeedTagAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        var maxId = await db.Tags.MaxAsync(t => (int?)t.TagID) ?? 0;
        var newId = Math.Max(100, maxId + 1);

        var tag = new Tag
        {
            TagID = newId,
            TagName = name,
            Note = "Note test"
        };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        _testTagIds.Add(tag.TagID);
        return tag.TagID;
    }

    private async Task<NewsArticle> SeedArticleAsync(
        string id,
        string title,
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
            Headline = $"Tóm tắt cho {title}",
            NewsContent = $"Nội dung bài viết {title}",
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
    public async Task Criterion_01_Related_ReturnsMax3_Distinct_ExcludesCurrent_OnlyActive()
    {
        // AC 1, AC 3: Tối đa 3, distinct, loại current, chỉ Active, sort mới nhất
        var now = DateTime.UtcNow;
        var cat1 = await SeedCategoryAsync("Chuyên mục Test Rel 1");
        var cat2 = await SeedCategoryAsync("Chuyên mục Test Rel 2");
        var tag1 = await SeedTagAsync("Tag Test Rel 1");
        var tag2 = await SeedTagAsync("Tag Test Rel 2");

        // Bài gốc: Category cat1, Tag tag1
        var current = await SeedArticleAsync("REL_CURR_01", "Bài viết gốc", cat1, true, 2, now.AddDays(-10), new List<int> { tag1 });

        // Bài A: Cùng Category cat1, Active, mới nhất (-1 day)
        await SeedArticleAsync("REL_ITEM_A", "Bài liên quan A", cat1, true, 2, now.AddDays(-1), new List<int> { tag2 });

        // Bài B: Khác Category (cat2), nhưng chung Tag tag1, Active (-2 days)
        await SeedArticleAsync("REL_ITEM_B", "Bài liên quan B", cat2, true, 2, now.AddDays(-2), new List<int> { tag1 });

        // Bài C: Cùng Category cat1 và chung Tag tag1, Active (-3 days)
        await SeedArticleAsync("REL_ITEM_C", "Bài liên quan C", cat1, true, 2, now.AddDays(-3), new List<int> { tag1 });

        // Bài D: Cùng Category cat1, Active, cũ hơn (-4 days) -> Phải bị loại bởi Take(3)
        await SeedArticleAsync("REL_ITEM_D", "Bài liên quan D", cat1, true, 2, now.AddDays(-4), new List<int>());

        // Bài E: Cùng Category cat1 và Tag tag1, NHƯNG INACTIVE (-0.5 days) -> TUYỆT ĐỐI không được trả về
        await SeedArticleAsync("REL_ITEM_E", "Bài Inactive không được hiện", cat1, false, 2, now.AddHours(-1), new List<int> { tag1 });

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/news/{current.NewsArticleID}/related");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var relatedList = await response.Content.ReadFromJsonAsync<List<NewsArticleDto>>();

        Assert.NotNull(relatedList);
        // Tối đa 3 bài
        Assert.Equal(3, relatedList.Count);

        // Distinct (mỗi bài có ID duy nhất)
        Assert.Equal(3, relatedList.Select(r => r.NewsArticleId).Distinct().Count());

        // Loại current bài viết gốc
        Assert.DoesNotContain(relatedList, r => r.NewsArticleId == "REL_CURR_01");

        // Chỉ Active
        Assert.All(relatedList, r => Assert.True(r.NewsStatus == true));
        Assert.DoesNotContain(relatedList, r => r.NewsArticleId == "REL_ITEM_E");

        // Đúng thứ tự mới nhất (A -> B -> C)
        Assert.Equal("REL_ITEM_A", relatedList[0].NewsArticleId);
        Assert.Equal("REL_ITEM_B", relatedList[1].NewsArticleId);
        Assert.Equal("REL_ITEM_C", relatedList[2].NewsArticleId);

        // Bài D bị loại bởi giới hạn 3
        Assert.DoesNotContain(relatedList, r => r.NewsArticleId == "REL_ITEM_D");
    }

    [Fact]
    public async Task Criterion_02_Related_MatchesByCategoryOrTag_CorrectOrPredicate()
    {
        // AC 2: Áp predicate chung đúng ngoặc OR: (cùng category || có ít nhất một tag)
        var now = DateTime.UtcNow;
        var catA = await SeedCategoryAsync("Chuyên mục Test CatA");
        var catB = await SeedCategoryAsync("Chuyên mục Test CatB");
        var tagA = await SeedTagAsync("Tag Test TagA");
        var tagB = await SeedTagAsync("Tag Test TagB");

        // Bài gốc: Category catA, Tag tagA
        var current = await SeedArticleAsync("REL_PRED_CURR", "Bài kiểm tra OR", catA, true, 2, now.AddDays(-10), new List<int> { tagA });

        // Bài 1: Cùng Category catA, khác tag (tagB) -> MATCH vì cùng Category
        await SeedArticleAsync("REL_PRED_CAT", "Bài cùng Category", catA, true, 2, now.AddDays(-2), new List<int> { tagB });

        // Bài 2: Khác Category (catB), cùng Tag tagA -> MATCH vì chung Tag
        await SeedArticleAsync("REL_PRED_TAG", "Bài cùng Tag", catB, true, 2, now.AddDays(-3), new List<int> { tagA });

        // Bài 3: Khác Category (catB), khác Tag (tagB) -> KHÔNG MATCH
        await SeedArticleAsync("REL_PRED_NONE", "Bài không liên quan", catB, true, 2, now.AddDays(-1), new List<int> { tagB });

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/news/{current.NewsArticleID}/related");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var relatedList = await response.Content.ReadFromJsonAsync<List<NewsArticleDto>>();

        Assert.NotNull(relatedList);
        Assert.Equal(2, relatedList.Count);
        Assert.Contains(relatedList, r => r.NewsArticleId == "REL_PRED_CAT");
        Assert.Contains(relatedList, r => r.NewsArticleId == "REL_PRED_TAG");
        Assert.DoesNotContain(relatedList, r => r.NewsArticleId == "REL_PRED_NONE");
    }

    [Fact]
    public async Task Criterion_03_Related_TargetArticleInactive_ReturnsNotFound()
    {
        // Anonymous/Public truy cập bài Inactive -> 404 Not Found (không để lộ liên quan hay metadata)
        var cat = await SeedCategoryAsync("Chuyên mục Test Inact");
        var inactive = await SeedArticleAsync("REL_INACT_01", "Bài viết tạm ẩn", cat, false, 2, DateTime.UtcNow, new List<int>());

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/news/{inactive.NewsArticleID}/related");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Criterion_04_Related_FewerThan3Results_ReturnsExactCount_NoFakeData()
    {
        // AC 4: Ít/không kết quả không dùng dữ liệu giả
        var now = DateTime.UtcNow;
        var cat = await SeedCategoryAsync("Chuyên mục Test Few");

        // Bài gốc: Category cat (không có tag nào)
        var current = await SeedArticleAsync("REL_FEW_CURR", "Bài ít liên quan", cat, true, 2, now.AddDays(-5), new List<int>());

        // Chỉ có đúng 1 bài khác cùng category cat
        await SeedArticleAsync("REL_FEW_ONE", "Bài liên quan duy nhất", cat, true, 2, now.AddDays(-1), new List<int>());

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/news/{current.NewsArticleID}/related");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var relatedList = await response.Content.ReadFromJsonAsync<List<NewsArticleDto>>();

        Assert.NotNull(relatedList);
        // Trả về đúng 1 bài thật, không độn thêm dữ liệu giả
        Assert.Single(relatedList);
        Assert.Equal("REL_FEW_ONE", relatedList[0].NewsArticleId);
    }

    [Fact]
    public async Task Criterion_04_Related_NoMatches_ReturnsEmptyList()
    {
        // AC 4: Không có bài liên quan -> trả về mảng rỗng []
        var cat = await SeedCategoryAsync("Chuyên mục Test Empty");
        var current = await SeedArticleAsync("REL_EMPTY_CURR", "Bài độc lập", cat, true, 2, DateTime.UtcNow, new List<int>());

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/news/{current.NewsArticleID}/related");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var relatedList = await response.Content.ReadFromJsonAsync<List<NewsArticleDto>>();

        Assert.NotNull(relatedList);
        Assert.Empty(relatedList);
    }
}
