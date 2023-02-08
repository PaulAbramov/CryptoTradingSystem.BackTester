using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Helper;
using CryptoTradingSystem.General.Strategy;

using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester
{
    internal static class Program
    {
        private const string StrategyDll = "StrategyDll";
        private const string ConnectionString = "ConnectionString";
        private const string LoggingLocation = "LoggingLocation";

        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var loggingfilePath = config.GetValue<string>(LoggingLocation);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
#if RELEASE
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
#endif
#if DEBUG
                .WriteTo.Console()
#endif
                .WriteTo.File(loggingfilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var strategyDll = config.GetValue<string>(StrategyDll);

            if (string.IsNullOrEmpty(strategyDll))
            {
                OverrideConfigFile(config);
            }

            strategyDll = config.GetValue<string>(StrategyDll);

            Log.Debug("Looking for strategy.dll in path: {strategyDll}", strategyDll);

            object obj;
            MethodInfo executeStrategyMethod;
            StrategyParameter strategyParameter;
            try
            {
                var DLLPath = new FileInfo(strategyDll);
                var assembly = Assembly.LoadFile(DLLPath.FullName);
                var t = assembly.GetTypes().First();
                obj = Activator.CreateInstance(t);
                var method = t.GetMethod("SetupStrategyParameter");
                executeStrategyMethod = t.GetMethod("ExecuteStrategy");

                strategyParameter = (StrategyParameter)method?.Invoke(obj, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load strategy.dll. Please check the Path");
                return;
            }

            if(strategyParameter.Assets.Count == 0)
            {
                Log.Error("No Assets requested in Strategyparameter.");
                return;
            }

            foreach(var asset in strategyParameter.Assets)
            {
                try
                {
                    IEnumerable<Indicator> indicatorsToCheck;
                    Type test = null;
                    switch (asset.Item3)
                    {
                        case Enums.Indicators.EMA:
                            test = typeof(EMA);
                            //indicatorsToCheck = Retry.Do(() => databaseHandler
                            //    .GetIndicators<EMA>(
                            //    asset.Item2,
                            //    asset.Item1,
                            //    asset.Item3),
                            //    TimeSpan.FromSeconds(1));
                            break;
                        case Enums.Indicators.SMA:
                            test = typeof(SMA);
                            //indicatorsToCheck = Retry.Do(() => databaseHandler
                            //    .GetIndicators<SMA>(
                            //    asset.Item2,
                            //    asset.Item1,
                            //    asset.Item3),
                            //    TimeSpan.FromSeconds(1));
                            break;
                        case Enums.Indicators.ATR:
                            test = typeof(ATR);
                            //indicatorsToCheck = Retry.Do(() => databaseHandler
                            //    .GetIndicators<ATR>(
                            //    asset.Item2,
                            //    asset.Item1,
                            //    asset.Item3),
                            //    TimeSpan.FromSeconds(1));
                            break;
                        default:
                            indicatorsToCheck = Enumerable.Empty<Indicator>();
                            break;
                    }

                    var databaseHandler = new MySQLDatabaseHandler(config.GetValue<string>(ConnectionString));

                    //make the call to "GetIndicators" Generic
                    var test2 = databaseHandler.GetType();

                    var method = test2.GetMethod("GetIndicators");
                    var genericMethod = method?.MakeGenericMethod(test);
                    var result = genericMethod?.Invoke(databaseHandler, null);

                    //Log.Debug("Received {amount} entries from the database.", indicatorsToCheck?.Count());
                }
                catch (Exception e)
                {
                    Log.Error(
                        e,
                        "{asset} | {timeFrame} | {indicator} | {lastClose} | could not get indicators from Database",
                        Enums.Assets.Btcusdt.GetStringValue(),
                        Enums.TimeFrames.M5.GetStringValue(),
                        Enums.Indicators.EMA.GetStringValue(),
                        DateTime.Now.AddMonths(-1));

                    throw;
                }
            }

            try
            {
                var targetString = (string)executeStrategyMethod?.Invoke(obj, null);
            }
            catch (Exception e)
            {
                Log.Error(e, "An Error appeared while executing the Strategy.");
            }
        }

        private static void OverrideConfigFile(IConfiguration config)
        {
            Log.Information("Enter Path to the strategy.dll:");

            config.GetSection(StrategyDll).Value = Console.In.ReadLine();

            var jsonWriteOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var configAsDict = config.AsEnumerable().ToDictionary(c => c.Key, c => c.Value);
            var json = JsonSerializer.Serialize(configAsDict, jsonWriteOptions);

            File.WriteAllText("appsettings.json", json);
        }
    }
}
