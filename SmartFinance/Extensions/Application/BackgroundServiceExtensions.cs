using Service.Services;

namespace SmartFinance.Extensions.Application;

public static class BackgroundServiceExtensions
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<DailySyncBackgroundService>();

        return services;
    }
}