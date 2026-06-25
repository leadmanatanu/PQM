using PQM.Core.DomainServices;
using PQM.Core.IRepositories;
using PQM.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddTransient<IDeviceService>(_ =>
    new DeviceService(connectionString));

builder.Services.AddTransient<IDeviceLogService>(_ =>
    new DeviceLogService(connectionString));

builder.Services.AddTransient<IParameterService>(_ =>
    new ParameterService(connectionString));

builder.Services.AddScoped<DeviceParameterService>(_ =>
    new DeviceParameterService(connectionString));

builder.Services.AddTransient<IFTPSettingService>(_ =>
    new FTPSettingService(connectionString));

builder.Services.AddTransient<IEventLogService>(_ =>
    new EventLogService(connectionString));

builder.Services.AddTransient<ISFTPService>(_ =>
    new SFTPService());

builder.Services.AddTransient<ICSVService>(_ =>
    new CSVService());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowReactApp");
app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();