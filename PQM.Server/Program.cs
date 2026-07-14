using PQM.Core.DomainServices;
using PQM.Core.IRepositories;
using PQM.Infrastructure.Repositories;
using PQM.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' or 'DATABASE_CONNECTION_STRING' not found."
    );

builder.Services.AddTransient<IDeviceService>(_ =>
    new DeviceService(connectionString));

builder.Services.AddSingleton<PQM.Infrastructure.Services.DLMSSessionManager>();

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

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// Auto-apply any pending EF Core migrations on startup (creates EventStatusMapping table etc.)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = new DataContext(connectionString);
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Migration error (non-fatal): {ex.Message}");
    }
}

app.UseCors("AllowReactApp");
app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

app.MapControllers();
app.MapHub<PQM.Server.Hubs.MeterHub>("/hubs/meter");
app.MapFallbackToFile("/index.html");

app.Run();