using Microsoft.Extensions.Configuration;
using PQM.Core.DomainServices;
using PQM.Core.IRepositories;
using PQM.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
builder.Services.AddOpenApi();
builder.Services.AddTransient<IDeviceService>(s => new DeviceService(config.GetSection("ConnectionString").Value));
builder.Services.AddTransient<IDeviceLogService>(s => new DeviceLogService(config.GetSection("ConnectionString").Value));
builder.Services.AddTransient<IParameterService>(s => new ParameterService(config.GetSection("ConnectionString").Value));
builder.Services.AddTransient<IDeviceParameterService>(s => new DeviceParameterService(config.GetSection("ConnectionString").Value));
builder.Services.AddTransient<IFTPSettingService>(s => new FTPSettingService(config.GetSection("ConnectionString").Value));
builder.Services.AddTransient<ISFTPService>(s => new SFTPService());
builder.Services.AddTransient<ICSVService>(s => new CSVService());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder.WithOrigins("*")
                          .AllowAnyMethod()
                          .AllowAnyHeader());
                          //.AllowCredentials());
});

var app = builder.Build();

app.UseCors("AllowReactApp");
app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
