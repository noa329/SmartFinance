using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.interfaces;
using Service.Interfaces;
using Service.Services;

namespace SmartFinance.Extensions.Application;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepository<User>, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IRepository<Budget>, BudgetRepository>();
        services.AddScoped<IBudgetService, BudgetService>();

        services.AddScoped<IRepository<Category>, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IRepository<Alert>, AlertRepository>();
        services.AddScoped<IAlertService, AlertService>();

        services.AddScoped<IRepository<Event>, EventRepository>();
        services.AddScoped<IEventService, EventService>();

        services.AddScoped<IRepository<Expense>, ExpenseRepository>();
        services.AddScoped<IExpenseService, ExpenseService>();

        return services;
    }
}