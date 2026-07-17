using PQM.Core.DomainServices;
using PQM.Core.IRepositories;
using PQM.Infrastructure.Repositories;
using PQM.Infrastructure;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddSingleton<PQM.Infrastructure.Services.DLMSSessionManager>();



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
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PQM API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

try
{
    app.MapControllers();
    app.MapHub<PQM.Server.Hubs.MeterHub>("/hubs/meter");
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