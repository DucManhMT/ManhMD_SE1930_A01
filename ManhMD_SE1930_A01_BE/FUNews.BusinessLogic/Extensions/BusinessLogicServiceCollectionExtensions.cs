using FUNews.BusinessLogic.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FUNews.BusinessLogic.Extensions;

public static class BusinessLogicServiceCollectionExtensions
{
    public static IServiceCollection AddFUNewsBusinessLogic(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<DefaultAdminOptions>(configuration.GetSection(DefaultAdminOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        // Register options as Singleton for direct injection (immutable configuration requirement)
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DefaultAdminOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppOptions>>().Value);

        // Register Scoped Application Services
        services.AddScoped<Security.IJwtTokenService, Security.JwtTokenService>();
        services.AddScoped<Services.IAuthService, Services.AuthService>();
        services.AddScoped<Services.ICategoryService, Services.CategoryService>();
        services.AddScoped<Services.ITagService, Services.TagService>();
        services.AddScoped<Services.INewsArticleService, Services.NewsArticleService>();
        services.AddScoped<Services.IAccountService, Services.AccountService>();

        return services;
    }
}
