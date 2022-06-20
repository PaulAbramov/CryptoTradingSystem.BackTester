using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using Microsoft.Extensions.Configuration;

namespace CryptoTradingSystem.BackTester
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var connectionString = config.GetValue<string>("ConnectionString");

            var databaseHandler = new MySQLDatabaseHandler(connectionString);

            var test = databaseHandler.GetIndicators<EMA>(Enums.Assets.Btcusdt, Enums.TimeFrames.M5, Enums.Indicators.EMA);
        }
    }
}
