using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;
using Serilog;

namespace PQM.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            string logDirectory = @"C:\PQM\Logs";
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "console-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            try
            {
                Log.Information("[PQM.Console] Starting PQM Meter Reader host...");

                var host = Host.CreateDefaultBuilder(args)
                    //.UseWindowsService(options =>
                    //{
                    //    options.ServiceName = "PQM Meter Reader";
                    //})
                    .UseSerilog()
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(AppContext.BaseDirectory);
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        string connectionString =
                            hostContext.Configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException(
                                "Connection string 'DefaultConnection' not found.");

                        int meterCooldown = hostContext.Configuration.GetValue<int>("DlmsSettings:MeterCooldownSeconds", 8);
                        DlmsMeterReader.DefaultMeterCooldownSeconds = meterCooldown > 0 ? meterCooldown : 8;

                        // Configure typed ConsoleOptions
                        services.Configure<PQM.Console.Options.ConsoleOptions>(options =>
                        {
                            options.DefaultConnection = connectionString;
                            options.ServerHubUrl = hostContext.Configuration["ServerHubUrl"] ?? "http://localhost:5135/hubs/device";
                            options.MeterCooldownSeconds = DlmsMeterReader.DefaultMeterCooldownSeconds;
                        });

                        // Register DataContext
                        services.AddScoped<DataContext>(sp =>
                            new DataContext(connectionString));

                        // Register Profile Sync Service
                        services.AddSingleton<ProfileSyncService>(sp =>
                            new ProfileSyncService(
                                connectionString,
                                sp.GetRequiredService<ILogger<ProfileSyncService>>()));

                        // Register Background Worker
                        services.AddHostedService<DeviceConsoleRunnerService>();
                    })
                    .Build();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[PQM.Console] Host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}