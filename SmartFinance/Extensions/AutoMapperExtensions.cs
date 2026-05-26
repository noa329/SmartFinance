using Service.Mapping;
using System.Reflection;

namespace SmartFinance.Extensions;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddMappingProfiles(this IServiceCollection services)
    {
        try
        {
            var types = typeof(MappingProfile).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                Console.WriteLine(loaderEx?.Message);
            }
        }

        try
        {
            services.AddAutoMapper(typeof(MappingProfile).Assembly);
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                Console.WriteLine("=== LOADER EXCEPTION ===");
                Console.WriteLine(loaderEx?.Message);
                Console.WriteLine(loaderEx?.InnerException?.Message);
            }
            throw;
        }

        return services;
    }
}