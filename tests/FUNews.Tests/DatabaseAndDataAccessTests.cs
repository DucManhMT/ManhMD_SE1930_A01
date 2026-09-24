using FUNews.BusinessLogic.Options;
using FUNews.DataAccess.Context;
using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Extensions;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Security;
using FUNews.DataAccess.Seeding;
using FUNews.DataAccess.Sequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace FUNews.Tests;

public class DatabaseAndDataAccessTests
{
    private const string ConnectionString = "Server=localhost;Database=FUNewsManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();

        var configurationData = new Dictionary<string, string?>
        {
            { "ConnectionStrings:FUNewsManagement", ConnectionString },
            { "DefaultAdmin:Email", "admin@FUNewsManagementSystem.org" },
            { "DefaultAdmin:Password", "@@abc123@@" },
            { "Jwt:Issuer", "FUNewsApi" },
            { "Jwt:Audience", "FUNewsWeb" },
            { "Jwt:SigningKey", "FUNewsManagement_PRN232_Secret_Key_For_Jwt_Auth_2026_Secure!" },
            { "App:TimeZone", "Asia/Ho_Chi_Minh" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddFUNewsDataAccess(ConnectionString);

        // Add Options
        services.Configure<DefaultAdminOptions>(configuration.GetSection(DefaultAdminOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DefaultAdminOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppOptions>>().Value);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AcceptanceCriteria_07_ServiceLifetimes_DbContextScoped_ConfigSingleton()
    {
        var provider = CreateTestServiceProvider();

        // Config Options should be Singleton
        var adminOptions1 = provider.GetRequiredService<DefaultAdminOptions>();
        var adminOptions2 = provider.GetRequiredService<DefaultAdminOptions>();
        Assert.Same(adminOptions1, adminOptions2);

        var jwtOptions1 = provider.GetRequiredService<JwtOptions>();
        var jwtOptions2 = provider.GetRequiredService<JwtOptions>();
        Assert.Same(jwtOptions1, jwtOptions2);

        // DbContext and DAOs/Repositories must be Scoped
        using (var scope1 = provider.CreateScope())
        using (var scope2 = provider.CreateScope())
        {
            var dbContext1 = scope1.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            var dbContext2 = scope2.ServiceProvider.GetRequiredService<FUNewsDbContext>();
            Assert.NotSame(dbContext1, dbContext2);

            var catRepo1 = scope1.ServiceProvider.GetRequiredService<ICategoryRepository>();
            var catRepo2 = scope2.ServiceProvider.GetRequiredService<ICategoryRepository>();
            Assert.NotSame(catRepo1, catRepo2);

            var catDao1 = scope1.ServiceProvider.GetRequiredService<CategoryDAO>();
            var catDao2 = scope2.ServiceProvider.GetRequiredService<CategoryDAO>();
            Assert.NotSame(catDao1, catDao2);
        }
    }

    [Fact]
    public void AcceptanceCriteria_06_PasswordHasher_CanHashAndVerify()
    {
        IPasswordHasher hasher = new BcryptPasswordHasher();

        string plain = "@1";
        string hash = hasher.HashPassword(plain);

        Assert.True(hasher.IsHashed(hash));
        Assert.False(hasher.IsHashed(plain));
        Assert.True(hasher.VerifyPassword(plain, hash));
        Assert.False(hasher.VerifyPassword("wrong_pwd", hash));
    }

    [Fact]
    public async Task AcceptanceCriteria_06_PasswordSeeder_SeedsIdempotently()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IPasswordSeeder>();
        var context = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Run seeder
        await seeder.SeedLegacyPasswordsAsync();

        // Verify all accounts have valid BCrypt hashes and match original demo password "@1"
        var accounts = await context.SystemAccounts.ToListAsync();
        Assert.NotEmpty(accounts);

        foreach (var account in accounts)
        {
            Assert.NotNull(account.AccountPassword);
            Assert.True(hasher.IsHashed(account.AccountPassword));
            Assert.True(hasher.VerifyPassword("@1", account.AccountPassword));
        }

        // Run seeder a second time: must update 0 rows and not double-hash
        int secondRunUpdated = await seeder.SeedLegacyPasswordsAsync();
        Assert.Equal(0, secondRunUpdated);
    }

    [Fact]
    public async Task AcceptanceCriteria_08_CanReadRealData_CategoryDesciptionTypoMapped()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var categories = await categoryRepo.GetAllAsync();

        Assert.True(categories.Count >= 5, "Database must contain at least 5 categories.");

        var cat1 = categories.FirstOrDefault(c => c.CategoryID == 1);
        Assert.NotNull(cat1);
        Assert.Equal("Academic news", cat1.CategoryName);
        Assert.NotEmpty(cat1.CategoryDescription); // Mapped from CategoryDesciption!

        var newsRepo = scope.ServiceProvider.GetRequiredService<INewsArticleRepository>();
        var news = await newsRepo.GetAllAsync();
        Assert.True(news.Count >= 5, "Database must contain at least 5 news articles.");

        var tagRepo = scope.ServiceProvider.GetRequiredService<ITagRepository>();
        var tags = await tagRepo.GetAllAsync();
        Assert.True(tags.Count >= 9, "Database must contain at least 9 tags.");

        var accountRepo = scope.ServiceProvider.GetRequiredService<ISystemAccountRepository>();
        var accounts = await accountRepo.GetAllAsync();
        Assert.True(accounts.Count >= 5, "Database must contain at least 5 accounts.");
    }

    [Fact]
    public async Task AcceptanceCriteria_03_ParentSeedIsValid_NoSelfReference()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var selfReferencing = await context.Categories
            .Where(c => c.ParentCategoryID != null && c.ParentCategoryID == c.CategoryID)
            .ToListAsync();

        Assert.Empty(selfReferencing);
    }

    [Fact]
    public async Task AcceptanceCriteria_05_Sequences_GenerateUniqueIncrementingValues()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var sequenceService = scope.ServiceProvider.GetRequiredService<ISqlSequenceService>();

        short accId1 = await sequenceService.GetNextAccountIdAsync();
        short accId2 = await sequenceService.GetNextAccountIdAsync();
        Assert.True(accId2 > accId1, "AccountID sequence should increment.");
        Assert.True(accId1 >= 6, "AccountID sequence should start greater than max seed (5).");

        int tagId1 = await sequenceService.GetNextTagIdAsync();
        int tagId2 = await sequenceService.GetNextTagIdAsync();
        Assert.True(tagId2 > tagId1, "TagID sequence should increment.");
        Assert.True(tagId1 >= 10, "TagID sequence should start greater than max seed (9).");

        string articleId1 = await sequenceService.GetNextNewsArticleIdAsync();
        string articleId2 = await sequenceService.GetNextNewsArticleIdAsync();
        Assert.NotEqual(articleId1, articleId2);
        Assert.StartsWith("N", articleId1);
        Assert.StartsWith("N", articleId2);
    }

    [Fact]
    public void AcceptanceCriteria_02_NoCascadeDelete_EFCoreMappingConfigured()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();
        var model = context.Model;

        var newsArticleType = model.FindEntityType(typeof(NewsArticle));
        Assert.NotNull(newsArticleType);

        var categoryFk = newsArticleType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Category));
        Assert.NotNull(categoryFk);
        Assert.Equal(DeleteBehavior.Restrict, categoryFk.DeleteBehavior);

        var accountFk = newsArticleType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(SystemAccount) && fk.Properties.Any(p => p.Name == "CreatedByID"));
        Assert.NotNull(accountFk);
        Assert.Equal(DeleteBehavior.Restrict, accountFk.DeleteBehavior);
    }

    [Fact]
    public async Task AcceptanceCriteria_04_UniqueConstraints_EnforcedInDatabase()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<FUNewsDbContext>();

        // Test duplicate email throws DbUpdateException due to unique index UQ_SystemAccount_AccountEmail
        var duplicateAccount = new SystemAccount
        {
            AccountID = 9999,
            AccountEmail = "EmmaWilliam@FUNewsManagement.org", // already in seed
            AccountName = "Duplicate Tester",
            AccountRole = 1,
            AccountPassword = "hash"
        };

        await context.SystemAccounts.AddAsync(duplicateAccount);
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public void Regression_AccountPassword_IsNeverExposedInJsonSerialization()
    {
        var account = new SystemAccount
        {
            AccountID = 1,
            AccountEmail = "test@example.com",
            AccountName = "Test User",
            AccountRole = 1,
            AccountPassword = "secret_hashed_password"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(account);

        Assert.DoesNotContain("AccountPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret_hashed_password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Regression_CategoryDAO_Delete_ReferencedCategory_Blocked()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var categoryDao = scope.ServiceProvider.GetRequiredService<CategoryDAO>();

        // Category 1 is Academic news and has existing news articles in seed
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await categoryDao.DeleteAsync(1);
        });

        Assert.Contains("referenced by existing news articles", ex.Message);
    }

    [Fact]
    public async Task Regression_SystemAccountDAO_Delete_ReferencedAccount_Blocked()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var accountDao = scope.ServiceProvider.GetRequiredService<SystemAccountDAO>();

        // Account 1 is Emma William, created articles in seed
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await accountDao.DeleteAsync(1);
        });

        Assert.Contains("referenced as creator of news articles", ex.Message);
    }

    [Fact]
    public async Task Regression_TagDAO_Delete_ReferencedTag_Blocked()
    {
        var provider = CreateTestServiceProvider();
        using var scope = provider.CreateScope();

        var tagDao = scope.ServiceProvider.GetRequiredService<TagDAO>();

        // Tag 1 is Education, attached to news articles in seed
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await tagDao.DeleteAsync(1);
        });

        Assert.Contains("associated with existing news articles", ex.Message);
    }
}

