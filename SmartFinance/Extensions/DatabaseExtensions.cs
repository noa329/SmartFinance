using DataContext;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;

namespace SmartFinance.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SmartFinanceContext>(options =>
            options.UseSqlServer(config.GetConnectionString("SmartFinanceDB")));

        services.AddScoped<IContext, SmartFinanceContext>();

        return services;
    }
}