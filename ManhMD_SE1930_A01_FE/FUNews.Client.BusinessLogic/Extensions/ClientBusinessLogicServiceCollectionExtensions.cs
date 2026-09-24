using FUNews.Client.BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FUNews.Client.BusinessLogic.Extensions;

public static class ClientBusinessLogicServiceCollectionExtensions
{
    public static IServiceCollection AddFUNewsClientBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<ICategoryClientService, CategoryClientService>();
        services.AddScoped<INewsClientService, NewsClientService>();
        services.AddScoped<ITagClientService, TagClientService>();
        services.AddScoped<IAccountClientService, AccountClientService>();

        return services;
    }
}
