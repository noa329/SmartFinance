using SmartFinance.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddControllers()
    .Services
    .AddSwaggerWithBearer()
    .AddReactCors()
    .AddApplicationServices(builder.Configuration)
    .AddMappingProfiles();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSmartFinanceMiddleware()
   .UseCors("AllowReactApp")
   .UseHttpsRedirection()
   .UseAuthentication()
   .UseAuthorization();

app.MapControllers();

app.Run();