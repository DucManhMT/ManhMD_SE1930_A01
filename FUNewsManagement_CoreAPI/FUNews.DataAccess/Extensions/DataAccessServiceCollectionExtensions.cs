using FUNews.DataAccess.Context;
using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Security;
using FUNews.DataAccess.Seeding;
using FUNews.DataAccess.Sequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FUNews.DataAccess.Extensions;

public static class DataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddFUNewsDataAccess(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        // DbContext registration with explicit Scoped lifetime
        services.AddDbContext<FUNewsDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        // Security / Password Hasher (Singleton as it is stateless)
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // Sequences & Seeding (Scoped)
        services.AddScoped<ISqlSequenceService, SqlSequenceService>();
        services.AddScoped<IPasswordSeeder, PasswordSeeder>();

        // DAOs (Scoped)
        services.AddScoped<CategoryDAO>();
        services.AddScoped<SystemAccountDAO>();
        services.AddScoped<TagDAO>();
        services.AddScoped<NewsArticleDAO>();
        services.AddScoped<NewsTagDAO>();

        // Repositories (Scoped)
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISystemAccountRepository, SystemAccountRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INewsArticleRepository, NewsArticleRepository>();
        services.AddScoped<INewsTagRepository, NewsTagRepository>();

        return services;
    }
}
