using Service.interfaces;
using Service.Interfaces;
using Service.Services;

namespace SmartFinance.Extensions.Application;

public static class CoreServicesExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<GmailInvoiceService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IInitialLoginService, InitialLoginService>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IReceiptParserService, ReceiptParserService>();
        services.AddScoped<IReceiptProcessorService, ReceiptProcessorService>();

        return services;
    }
}