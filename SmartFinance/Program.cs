using Common.Configurations;
using DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.interfaces;
using Service.Interfaces;
using Service.Mapping;
using Service.Services;
using System.Reflection;
using System.Text;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// ================= DB Configuration =================
builder.Services.AddDbContext<SmartFinanceContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SmartFinanceDB")
    ));

builder.Services.AddScoped<IContext, SmartFinanceContext>();

// ================= Controllers + Swagger =================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token like this: Bearer {your token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ================= JWT Authentication =================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
{
    throw new Exception("Google Authentication credentials are missing!");
}

// ================= Application Services =================

// 1. הגדרות מערכת (Options)
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 2. תשתית מיילים (רשום כעת תחת הממשק החדש שלו)
// לרשום את שניהם כדי שכל שירות ימצא את מה שהוא מחפש
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<EmailService>();
// 3. לקוחות HTTP מוגדרים (Typed Http Clients)
builder.Services.AddHttpClient<AiInvoiceParserService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});

builder.Services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

// 4. שירותי ליבה של המערכת
builder.Services.AddScoped<GmailInvoiceService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IInitialLoginService, InitialLoginService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IReceiptParserService, ReceiptParserService>();
builder.Services.AddScoped<IReceiptProcessorService, ReceiptProcessorService>();

// 5. רישום Repositories ו-Services עבור ישויות ה-DB
builder.Services.AddScoped<IReceiptParserService, ReceiptParserService>();

builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IRepository<Budget>, BudgetRepository>();
builder.Services.AddScoped<IBudgetService, BudgetService>();

builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<IRepository<Alert>, AlertRepository>();
builder.Services.AddScoped<IAlertService, AlertService>();

builder.Services.AddScoped<IRepository<Event>, EventRepository>();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddScoped<IRepository<Expense>, ExpenseRepository>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

// 6. שירותי רקע (Background Services)
builder.Services.AddHostedService<DailySyncBackgroundService>();

// ================= AutoMapper Validation =================
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
    builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
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

// ================= CORS Configuration =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// ================= Build App =================
var app = builder.Build();

// ================= Middleware Pipeline =================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();