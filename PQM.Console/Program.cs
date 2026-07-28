using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;

namespace PQM.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // Create the Host Builder
            var builder = Host.CreateDefaultBuilder(args);

            // If running as a Windows Service, enable Windows Service support.
            // When debugging from Visual Studio, it will continue running as a console app.
            if (!Environment.UserInteractive)
            {
                builder.UseWindowsService(options =>
                {
                    options.ServiceName = "PQM Meter Reader";
                });
            }

            var host = builder
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
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();

                    // Console logs while debugging
                    logging.AddConsole();

                    // Event Viewer logs when running as a Windows Service
                    logging.AddEventLog(settings =>
                    {
                        settings.SourceName = "PQM Meter Reader";
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}