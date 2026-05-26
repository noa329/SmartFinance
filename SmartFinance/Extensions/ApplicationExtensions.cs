using SmartFinance.Extensions.Application;

namespace SmartFinance.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddEmailServices(config)
            .AddHttpClients()
            .AddCoreServices()
            .AddRepositories()
            .AddBackgroundServices();

        return services;
    }
}