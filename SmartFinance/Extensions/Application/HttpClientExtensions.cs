using Service.Interfaces;
using Service.Services;

namespace SmartFinance.Extensions.Application;

public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<AiInvoiceParserService>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        });

        services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

        return services;
    }
}