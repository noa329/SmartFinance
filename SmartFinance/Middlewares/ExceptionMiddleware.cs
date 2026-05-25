using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartFinance.Exceptions; 

namespace SmartFinance.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // ממשיך את הבקשה כרגיל ב-Pipeline
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // רושם את השגיאה המלאה ב-Log של השרת לצורכי דיבאג
                _logger.LogError(ex, $"An unhandled exception occurred: {ex.Message}");

                // מטפל בשגיאה ומחזיר תשובה מסודרת ללקוח (React)
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // התאמת סטטוס קוד ה-HTTP לפי סוג ה-Exception שנזרק
            context.Response.StatusCode = exception switch
            {
                BadRequestException => (int)HttpStatusCode.BadRequest,       // 400 - עבור אימייל קיים או שגיאות לוגיות ספציפיות שלך
                ArgumentException => (int)HttpStatusCode.BadRequest,         // 400 - עבור חריגת אחוזים בתקציב (100%)
                KeyNotFoundException => (int)HttpStatusCode.NotFound,       // 404 - אם ישות לא נמצאה
                _ => (int)HttpStatusCode.InternalServerError                // 500 - שגיאות קוד לא צפויות אחרות
            };

            // יצירת אובייקט תשובה אחיד שה-Frontend ידע לקרוא בקלות
            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = exception.Message // כאן יעבור הטקסט המדויק שכתבת בתוך ה-throw new
            };

            // המרה ל-JSON עם תמיכה ב-camelCase (אותיות קטנות בהתחלה עבור ה-JS)
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}