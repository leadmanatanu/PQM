using PQM.Infrastructure.Services;
using PQM.Core.IRepositories;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure.Repositories;
using PQM.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{   
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddTransient<IDeviceService>(_ =>
    new DeviceService(connectionString));

builder.Services.AddScoped<DataContext>(provider => new DataContext(connectionString));
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
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

// Auto-apply any pending EF Core migrations on startup
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

// Auto-start PQMMeterReader Windows Service if currently stopped
try
{
    if (OperatingSystem.IsWindows())
    {
        using var sc = new System.ServiceProcess.ServiceController("PQMMeterReader");
        if (sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped ||
            sc.Status == System.ServiceProcess.ServiceControllerStatus.StopPending)
        {
            Console.WriteLine("[Startup] PQMMeterReader Windows service is stopped. Attempting auto-start...");
            sc.Start();
            Console.WriteLine("[Startup] PQMMeterReader service start command sent successfully.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Note on PQMMeterReader Windows service auto-start: {ex.Message}");
}

app.UseCors("AllowReactApp");
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PQM API v1");
    });
}

// app.UseHttpsRedirection();
app.UseAuthorization();

try
{
    app.MapControllers();
    app.MapHub<PQM.Server.Hubs.MeterHub>("/hubs/meter");
    app.MapHub<PQM.Server.Hubs.DeviceHub>("/hubs/device");
    app.MapFallbackToFile("/index.html");

    app.Run();
}
catch (System.Reflection.ReflectionTypeLoadException ex)
{
    Console.WriteLine("=== REFLECTION TYPE LOAD EXCEPTION ===");
    foreach (var le in ex.LoaderExceptions)
    {
        Console.WriteLine($"[LoaderException]: {le?.Message}");
    }
    throw;
}