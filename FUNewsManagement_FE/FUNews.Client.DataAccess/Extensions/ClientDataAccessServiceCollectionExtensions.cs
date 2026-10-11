using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Handlers;
using FUNews.Client.DataAccess.Policies;
using FUNews.Client.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FUNews.Client.DataAccess.Extensions;

public static class ClientDataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddFUNewsClientDataAccess(this IServiceCollection services, string apiBaseUrl)
    {
        return services.AddFUNewsClientDataAccess(apiBaseUrl, apiBaseUrl, apiBaseUrl);
    }

    public static IServiceCollection AddFUNewsClientDataAccess(
        this IServiceCollection services,
        string coreApiUrl,
        string analyticsApiUrl,
        string aiApiUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coreApiUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(analyticsApiUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(aiApiUrl);

        var coreUri = new Uri(coreApiUrl.EndsWith('/') ? coreApiUrl : $"{coreApiUrl}/");
        var analyticsUri = new Uri(analyticsApiUrl.EndsWith('/') ? analyticsApiUrl : $"{analyticsApiUrl}/");
        var aiUri = new Uri(aiApiUrl.EndsWith('/') ? aiApiUrl : $"{aiApiUrl}/");

        // Đăng ký Handlers
        services.AddTransient<AuthTokenDelegatingHandler>();
        services.AddTransient<AuthHeaderHandler>();

        // Đăng ký Refresh Client chuyên dụng (không gắn delegating handler để tránh đệ quy)
        services.AddHttpClient("AuthRefreshClient", client =>
        {
            client.BaseAddress = coreUri;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // 1. Core API Client (Quản lý nghiệp vụ chính)
        services.AddHttpClient<ICoreApiClient, CoreApiClient>(client =>
        {
            client.BaseAddress = coreUri;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthTokenDelegatingHandler>()
        .AddPolicyHandler((_, req) => PollyPolicies.GetSelectableRetryPolicy(req));

        // Alias tương thích ngược cho IFUNewsApiClient từ A01
        services.AddScoped<IFUNewsApiClient>(sp => sp.GetRequiredService<ICoreApiClient>());

        // 2. Analytics API Client (Dashboard & Báo cáo)
        services.AddHttpClient<IAnalyticsApiClient, AnalyticsApiClient>(client =>
        {
            client.BaseAddress = analyticsUri;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthTokenDelegatingHandler>()
        .AddPolicyHandler((_, req) => PollyPolicies.GetSelectableRetryPolicy(req));

        // 3. AI API Client (Gợi ý Tag & Learning Cache)
        services.AddHttpClient<IAiApiClient, AiApiClient>(client =>
        {
            client.BaseAddress = aiUri;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthTokenDelegatingHandler>()
        .AddPolicyHandler((_, req) => PollyPolicies.GetSelectableRetryPolicy(req));

        // 4. Offline Store & Background Worker (6h HostedService)
        services.AddSingleton<IOfflineStoreManager, OfflineStoreManager>();
        services.AddHostedService<CacheRefreshWorker>();

        return services;
    }
}
