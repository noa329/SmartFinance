using DataContext;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.interfaces;
using Service.Interfaces;
using Service.Mapping;
using Service.sevices;
using Service.Sevices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SmartFinanceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartFinanceDB")));


// Add services to the container.

//builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connection = builder.Configuration.GetConnectionString("SmartFinanceDB");

//builder.Services.AddSingleton<IContext>(new SmartFinanceContext(connection));
builder.Services.AddScoped<IContext, SmartFinanceContext>();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IRepository<Budget>, BudgetRepository>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRepository<User>, UserRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}




app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
