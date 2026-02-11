using DataContext;
using Microsoft.EntityFrameworkCore;
using Repository;
using Service.Mapping;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SmartFinanceContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartFinanceDB")));


// Add services to the container.
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddControllers();
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
