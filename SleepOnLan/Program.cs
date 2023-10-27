using Serilog;

namespace SleepOnLan
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(path: "logging.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new Serilog.LoggerConfiguration().ReadFrom.Configuration(configuration, new Serilog.Settings.Configuration.ConfigurationReaderOptions() { SectionName = "Serilog" }).CreateLogger();

            try
            {
                Log.Information("SleepOnLan started");
                var builder = CreateHostBuilder(args);
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
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