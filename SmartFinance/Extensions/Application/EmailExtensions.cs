using Common.Configurations;
using Service.interfaces;
using Service.Interfaces;
using Service.Services;

namespace SmartFinance.Extensions.Application;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<EmailService>();

        return services;
    }
}