using FUNews.Client.DataAccess.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace FUNews.Client.DataAccess.Extensions;

public static class ClientDataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddFUNewsClientDataAccess(this IServiceCollection services, string apiBaseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiBaseUrl);

        var baseAddress = new Uri(apiBaseUrl.EndsWith('/') ? apiBaseUrl : $"{apiBaseUrl}/");

        services.AddTransient<AuthHeaderHandler>();

        services.AddHttpClient<IFUNewsApiClient, FUNewsApiClient>(client =>
        {
            client.BaseAddress = baseAddress;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }
}
