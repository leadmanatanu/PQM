using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQM.Infrastructure;
using PQM.Infrastructure.Services;

namespace PQM.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            System.Console.WriteLine("=================================================");
            System.Console.WriteLine(" PQM Production Sync Runner (PQM.Console)");
            System.Console.WriteLine(" Dedicated DLMS Meter Synchronization Process");
            System.Console.WriteLine("=================================================");

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    string connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                    // Register DbContext & Repositories/Services needed for DLMS Meter Sync
                    services.AddScoped<DataContext>(sp => new DataContext(connectionString));
                    services.AddSingleton<ProfileSyncService>(sp =>
                        new ProfileSyncService(connectionString, sp.GetRequiredService<ILogger<ProfileSyncService>>()));

                    // Register Sync Runner Hosted Service
                    services.AddHostedService<DeviceConsoleRunnerService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
