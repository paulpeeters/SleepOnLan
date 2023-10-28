using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Serilog;
using System.Diagnostics.Metrics;

namespace SleepOnLan
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(path: "config/logging.json", optional: false, reloadOnChange: true)
                .Build();
            
            Log.Logger = new Serilog.LoggerConfiguration()
                .ReadFrom
                .Configuration(configuration, new Serilog.Settings.Configuration.ConfigurationReaderOptions() { SectionName = "Serilog" })
                .CreateLogger();

            try
            {
                Log.Information("SleepOnLan started");
                var builder = CreateHostBuilder(args, configuration);
                var host = builder.Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception");
            }
            finally
            {
                Log.Information("SleepOnLan stopped");
                Log.CloseAndFlush();
            }
        }

        private static void SetupConfiguration(HostBuilderContext context, IConfiguration configuration)
        {
        }
        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    // this is a way to read all appsettings.json files from config subfolder instead of application root folder
                    foreach(var item in builder.Sources)
                    {
                        if (item.GetType() == typeof(JsonConfigurationSource))
                        {
                            JsonConfigurationSource? jsonConfigurationSource = item as JsonConfigurationSource;
                            if (jsonConfigurationSource != null && 
                            !string.IsNullOrEmpty(jsonConfigurationSource.Path) 
                            && jsonConfigurationSource.Path.StartsWith("appsettings", StringComparison.InvariantCultureIgnoreCase))
                            {
                                jsonConfigurationSource.Path = jsonConfigurationSource.Path.Insert(0, "config/");
                            }
                        }
                    }
                    builder.AddConfiguration(configuration);
                    builder.AddJsonFile(path: "config/appsettings.json", optional: true, reloadOnChange: true);
                    builder.AddJsonFile(path: $"config/appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                services
                    .AddSingleton<SleepService>()
                    .AddHostedService<UdpService>()
                    ;
                })
                .UseWindowsService()
                .UseSystemd();

            return builder;
        }
    }
}