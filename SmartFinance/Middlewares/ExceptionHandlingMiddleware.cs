using System.Net;
using System.Text.Json;
using System.Text.Encodings.Web;
using Service.Exceptions; 

namespace SmartFinance.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            string message = "התרחשה שגיאה פנימית בשרת.";

            // 1. זיהוי סוג השגיאה
            if (exception is BadRequestException || exception.GetType().Name == "BadRequestException")
            {
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                _logger.LogWarning($"שגיאת לקוח מנוהלת: {message}");
            }
            else
            {
                _logger.LogError(exception, $"קריסת שרת לא צפויה: {exception.Message}");
            }

            // 2. הגדרת כותרות התשובה (Headers)
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)statusCode;

            // 3. הגדרות סיריאליזציה קריטיות - תמיכה מלאה בטקסט בעברית
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // ✅ מונע מהעברית להפוך לג'יבריש/לקרוס
            };

            var responseObj = new
            {
                statusCode = (int)statusCode,
                message = message
            };

            var jsonResult = JsonSerializer.Serialize(responseObj, jsonOptions);

            return context.Response.WriteAsync(jsonResult);
        }
    }
}