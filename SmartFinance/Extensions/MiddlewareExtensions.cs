using SmartFinance.Middlewares;

namespace SmartFinance.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseSmartFinanceMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<ExceptionMiddleware>();

        return app;
    }
}