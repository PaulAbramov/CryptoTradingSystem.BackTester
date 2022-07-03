using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var loggingfilePath = config.GetValue<string>("LoggingLocation");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(loggingfilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var connectionString = config.GetValue<string>("ConnectionString");
            var databaseHandler = new MySQLDatabaseHandler(connectionString);

            var test = databaseHandler.GetIndicators<EMA>(Enums.Assets.Btcusdt, Enums.TimeFrames.M5, Enums.Indicators.EMA);
        }
    }
}
