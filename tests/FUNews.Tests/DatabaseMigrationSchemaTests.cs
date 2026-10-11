using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FUNews.Tests;

public class DatabaseMigrationSchemaTests : IAsyncLifetime
{
    private const string ConnectionString = "Server=localhost;Database=FUNewsManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private static FUNewsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FUNewsDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new FUNewsDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await CleanupTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestDataAsync();
    }

    private static async Task CleanupTestDataAsync()
    {
        using var context = CreateDbContext();

        var testAuditLogs = await context.AuditLogs
            .Where(a => a.EntityID == "MIG_ART_01" || a.EntityID == "MIG_ART_02")
            .ToListAsync();
        if (testAuditLogs.Count > 0)
        {
            context.AuditLogs.RemoveRange(testAuditLogs);
        }

        var testViews = await context.NewsViews
            .Where(v => v.NewsArticleID == "MIG_ART_01")
            .ToListAsync();
        if (testViews.Count > 0)
        {
            context.NewsViews.RemoveRange(testViews);
        }

        var testArticle = await context.NewsArticles
            .FirstOrDefaultAsync(a => a.NewsArticleID == "MIG_ART_01");
        if (testArticle != null)
        {
            context.NewsArticles.Remove(testArticle);
        }

        var testLearning = await context.TagLearnings
            .FirstOrDefaultAsync(tl => tl.TagID == 99999);
        if (testLearning != null)
        {
            context.TagLearnings.Remove(testLearning);
        }

        var testTag = await context.Tags
            .FirstOrDefaultAsync(t => t.TagID == 99999);
        if (testTag != null)
        {
            context.Tags.Remove(testTag);
        }

        var testTokens = await context.RefreshTokens
            .Where(t => t.Token.StartsWith("mig.test.token."))
            .ToListAsync();
        if (testTokens.Count > 0)
        {
            context.RefreshTokens.RemoveRange(testTokens);
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AuditLog_ShouldBePersistedAndQueriedInSqlServer()
    {
        using var context = CreateDbContext();

        var log = new AuditLog
        {
            UserID = 1,
            UserName = "admin@funews.com",
            Action = "CREATE",
            Entity = "NewsArticle",
            EntityID = "MIG_ART_01",
            BeforeJSON = null,
            AfterJSON = "{\"Title\":\"Tiêu đề kiểm thử di trú\"}",
            Timestamp = DateTime.UtcNow
        };

        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();

        var persisted = await context.AuditLogs.FirstOrDefaultAsync(l => l.EntityID == "MIG_ART_01");
        Assert.NotNull(persisted);
        Assert.True(persisted.AuditID > 0);
        Assert.Equal("CREATE", persisted.Action);
        Assert.Equal("NewsArticle", persisted.Entity);
        Assert.Equal("admin@funews.com", persisted.UserName);
        Assert.NotNull(persisted.AfterJSON);
    }

    [Fact]
    public async Task NewsArticle_NewsImage_ShouldBePersistedAndQueriedInSqlServer()
    {
        using var context = CreateDbContext();

        var article = new NewsArticle
        {
            NewsArticleID = "MIG_ART_01",
            Headline = "Tiêu đề phụ kiểm thử ảnh",
            NewsTitle = "Bài viết kiểm thử cột NewsImage",
            NewsImage = "/uploads/news/2026/migration-sample.png",
            CreatedDate = DateTime.UtcNow,
            NewsStatus = true
        };

        context.NewsArticles.Add(article);
        await context.SaveChangesAsync();

        var persisted = await context.NewsArticles.FindAsync("MIG_ART_01");
        Assert.NotNull(persisted);
        Assert.Equal("/uploads/news/2026/migration-sample.png", persisted.NewsImage);
    }

    [Fact]
    public async Task NewsView_ShouldTrackViewsWithCascadeToNewsArticle()
    {
        using var context = CreateDbContext();

        var article = new NewsArticle
        {
            NewsArticleID = "MIG_ART_01",
            Headline = "Tiêu đề phụ",
            NewsTitle = "Bài viết kiểm thử NewsView",
            CreatedDate = DateTime.UtcNow,
            NewsStatus = true
        };
        context.NewsArticles.Add(article);
        await context.SaveChangesAsync();

        var view = new NewsView
        {
            NewsArticleID = "MIG_ART_01",
            VisitorIP = "192.168.1.100",
            ViewedAt = DateTime.UtcNow
        };
        context.NewsViews.Add(view);
        await context.SaveChangesAsync();

        var persistedView = await context.NewsViews.FirstOrDefaultAsync(v => v.NewsArticleID == "MIG_ART_01");
        Assert.NotNull(persistedView);
        Assert.True(persistedView.ViewID > 0);
        Assert.Equal("192.168.1.100", persistedView.VisitorIP);
    }

    [Fact]
    public async Task TagLearning_ShouldStoreSelectionCountWithRelationToTag()
    {
        using var context = CreateDbContext();

        var tag = new Tag
        {
            TagID = 99999,
            TagName = "MigrationTestTag",
            Note = "Test tag for A02 migration"
        };
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var learning = new TagLearning
        {
            TagID = 99999,
            SelectionCount = 12,
            LastSelectedAt = DateTime.UtcNow
        };
        context.TagLearnings.Add(learning);
        await context.SaveChangesAsync();

        var persistedLearning = await context.TagLearnings
            .Include(tl => tl.Tag)
            .FirstOrDefaultAsync(tl => tl.TagID == 99999);

        Assert.NotNull(persistedLearning);
        Assert.Equal(12, persistedLearning.SelectionCount);
        Assert.Equal("MigrationTestTag", persistedLearning.Tag.TagName);
    }

    [Fact]
    public async Task RefreshToken_ShouldSupportAdminConfig_WithUserEmail()
    {
        using var context = CreateDbContext();

        var tokenString = $"mig.test.token.{Guid.NewGuid():N}";
        var token = new RefreshToken
        {
            AccountID = null,
            UserEmail = "admin@FUNewsManagementSystem.org",
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var persisted = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenString);
        Assert.NotNull(persisted);
        Assert.True(persisted.TokenID > 0);
        Assert.Null(persisted.AccountID);
        Assert.Equal("admin@FUNewsManagementSystem.org", persisted.UserEmail);
        Assert.False(persisted.IsRevoked);
    }
}
