using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQM.Core.Events;
using PQM.Core.Interfaces.Repositories;
using PQM.Infrastructure;
using PQM.Infrastructure.Events;
using PQM.Infrastructure.Repositories;
using PQM.Infrastructure.Services;

namespace PQM.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                //.UseWindowsService(options =>
                //{
                //    options.ServiceName = "PQM Meter Reader";
                //})
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile(
                        "appsettings.json",
                        optional: false,
                        reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    string connectionString =
                        hostContext.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException(
                            "Connection string 'DefaultConnection' not found.");

                    services.AddDbContext<DataContext>(options =>
                        options.UseSqlServer(connectionString));

                    int meterCooldown =
                        hostContext.Configuration.GetValue<int>(
                            "DlmsSettings:MeterCooldownSeconds", 8);

                    DlmsMeterReader.DefaultMeterCooldownSeconds =
                        meterCooldown > 0 ? meterCooldown : 8;

                    services.Configure<ConsoleOptions>(options =>
                    {
                        options.DefaultConnection = connectionString;
                        options.MeterCooldownSeconds =
                            DlmsMeterReader.DefaultMeterCooldownSeconds;
                    });

                    services.AddScoped<IDeviceRepository, DeviceRepository>();
                    services.AddScoped<INetworkReachabilityService, NetworkReachabilityService>();
                    services.AddScoped<ISyncRunLogRepository, SyncRunLogRepository>();

                    services.AddSingleton<IEventPublisher, EventPublisher>();

                    services.AddScoped<ProfileSyncService>(sp =>
                        new ProfileSyncService(
                            connectionString,
                            sp.GetRequiredService<ILogger<ProfileSyncService>>(),
                            sp.GetRequiredService<IEventPublisher>(),
                            sp.GetRequiredService<ISyncRunLogRepository>()));

                    services.AddHostedService<DeviceConsoleRunnerService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}