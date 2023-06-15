using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester
{
    internal static class Program
    {
        private static void Main()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile(SettingsHelper.AppsettingsFile).Build();

            var loggingfilePath = config.GetValue<string>(SettingsHelper.LoggingLocation);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
#if RELEASE
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
#endif
#if DEBUG
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
#endif
                .WriteTo.File(loggingfilePath ?? "logs/Backtester.txt", 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var menu = new MainMenu(config);
            menu.StartMainMenu();
            
            menu.Dispose();
        }
    }
}
