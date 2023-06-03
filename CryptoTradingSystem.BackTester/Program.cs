using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;

using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester
{
    internal static class Program
    {
        private const string strategyDll = "StrategyDll";
        private const string connectionString = "ConnectionString";
        private const string loggingLocation = "LoggingLocation";

        private static int cursor = 0;
        private static List<int> selectedDllIndex = new List<int>();

        private static void Main()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var loggingfilePath = config.GetValue<string>(loggingLocation);
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

            
            var dllPathsSection = config.GetSection("DllPaths");
            var dllPaths = dllPathsSection.GetChildren().ToDictionary(x => x.Key, x => x.Value);

            var menu = new MainMenu();
            menu.StartMainMenu(config);
            return;
            
while (true)
{
    Console.WriteLine("Select an option:");
    Console.WriteLine("1. Add .dll file path");
    Console.WriteLine("2. Remove .dll file path");
    Console.WriteLine("3. Execute selected .dll files");
    Console.WriteLine("4. Exit");

    var option = Console.ReadLine();

    switch (option)
    {
        case "1":
            Console.WriteLine("Enter the path to the .dll file:");
            var test = Console.ReadLine();
            dllPaths[(new FileInfo(test)).Name] = test;
            //UpdateAppSettings(dllPaths);
            Console.WriteLine("Path added successfully!");
            break;
        case "2":
            Console.WriteLine("Enter the name of the .dll file to remove:");
            var dllToRemove = Console.ReadLine();
            if (dllPaths.ContainsKey(dllToRemove))
            {
                dllPaths.Remove(dllToRemove);
                //UpdateAppSettings(dllPaths);
                Console.WriteLine("Path removed successfully!");
            }
            else
            {
                Console.WriteLine($"No path found for {dllToRemove}.");
            }
            break;
        case "3":
            Console.WriteLine("Enter the names of the .dll files you want to execute (comma-separated):");
            var selectedDlls = Console.ReadLine()?.Split(',');

            // Execute the selected .dll files
            foreach (var selectedDll in selectedDlls)
            {
                if (dllPaths.TryGetValue(selectedDll.Trim(), out var dllPath))
                {
                    // Execute the .dll file using appropriate mechanism (e.g., reflection)
                }
                else
                {
                    Console.WriteLine($"Invalid .dll file: {selectedDll}");
                }
            }
            break;
        case "4":
            Console.WriteLine("Exiting...");
            return;
        default:
            Console.WriteLine("Invalid option. Please try again.");
            break;
    }
}
            
            
            // Get the path to the strategy.dll
            var strategyDll = GetStrategyDllPath(config);
            if (strategyDll is null)
            {
                Log.Error("No path to strategyDLL found in appsettings.json, please check the file");
                return;
            }
            
            var connectionString = config.GetValue<string>(Program.connectionString);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Log.Error("No ConnectionString found in appsettings.json, please check the file");
                return;
            }
            
            ExecuteStrategy(strategyDll, connectionString);
        }
        
        private static string? GetStrategyDllPath(IConfiguration config)
        {
            var strategyDll = config.GetValue<string>(Program.strategyDll);

            if (string.IsNullOrEmpty(strategyDll) 
                || !File.Exists(strategyDll))
            {
                OverrideConfigFile(config);
            }

            strategyDll = config.GetValue<string>(Program.strategyDll);

            Log.Debug("Looking for strategy.dll in path: {StrategyDll}", strategyDll);

            return strategyDll;
        }

        private static void OverrideConfigFile(IConfiguration config)
        {
            Log.Information("Enter path (including the .dll) to the strategy.dll:");

            config.GetSection(strategyDll).Value = Console.In.ReadLine();

            var jsonWriteOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var configAsDict = config.AsEnumerable().ToDictionary(c => c.Key, c => c.Value);
            var json = JsonSerializer.Serialize(configAsDict, jsonWriteOptions);

            File.WriteAllText("appsettings.json", json);
        }

        private static void ExecuteStrategy(string strategyDll, string connectionString)
        {
            // Load methods "ExecuteStrategy" and "SetupStrategyParameter" from strategy.dll
            StrategyParameter strategyParameter = default;
            MethodInfo? executeStrategyMethod = default;
            object? obj = default;
            try
            {
                var dllPath = new FileInfo(strategyDll);
                var assembly = Assembly.LoadFile(dllPath.FullName);
                var types = assembly.GetTypes();
                var t = types.FirstOrDefault(x => x.Name is "Strategy");
                if (t is null)
                {
                    Log.Error("Could not load strategy.dll. Please check the Path");
                    return;
                }
                
                obj = Activator.CreateInstance(t) ?? throw new InvalidOperationException();
                
                var method = t.GetMethod("SetupStrategyParameter");
                executeStrategyMethod = t.GetMethod("ExecuteStrategy");
                if (executeStrategyMethod is null || method is null)
                {
                    Log.Error("Could not load methods 'SetupStrategyParameter' and 'ExecuteStrategy' from strategy.dll. " +
                              "Please check the Strategy");
                    return;
                }

                // execute SetupStrategyParameter
                strategyParameter = (StrategyParameter)(method?.Invoke(obj, null) ?? throw new InvalidOperationException());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load strategy.dll. Please check the Path");
                return;
            }
            
            if(strategyParameter.Assets.Count == 0)
            {
                Log.Error("No Assets requested in Strategyparameter");
                return;
            }

            var tradestatus = Enums.TradeStatus.Closed;
            while (true)
            {
                var results = GetDataFromDatabase(strategyParameter, connectionString);

                try
                {
                    var tradeType = (Enums.TradeType) executeStrategyMethod?.Invoke(obj, new object?[]{results, tradestatus})!;
                    switch (tradeType)
                    {
                        case Enums.TradeType.None:
                            break;
                        case Enums.TradeType.Buy:
                            Log.Warning("Buy {AssetName} at {CloseTime}| Price: {CandleClose}",
                                strategyParameter.AssetToBuy,
                                results[0].CloseTime,
                                results[0].Asset?.CandleClose);
                            tradestatus = Enums.TradeStatus.Open;
                            break;
                        case Enums.TradeType.Sell:
                            Log.Warning("Sell {AssetName} at {CloseTime} | Price: {CandleClose}",
                                strategyParameter.AssetToBuy,
                                results[0].CloseTime,
                                results[0].Asset?.CandleClose);
                            tradestatus = Enums.TradeStatus.Closed;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    
                    strategyParameter.TimeFrameStart = results.Min(x => x.CloseTime);
                }
                catch (Exception e)
                {
                    Log.Error(e, "An Error appeared while executing the Strategy");
                    return;
                }
            }
        }

        /// <summary>
        /// iterates over all assets and gets the data from the database
        /// </summary>
        /// <param name="strategyParameter"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static List<Indicator> GetDataFromDatabase(StrategyParameter strategyParameter, string connectionString)
        {
            var results = new List<Indicator>();
            foreach(var asset in strategyParameter.Assets)
            {
                try
                {
                    var databaseHandler = new MySQLDatabaseHandler(connectionString);

                    //make the call to "GetIndicators" Generic
                    var databaseHandlerType = databaseHandler.GetType();

                    var method = databaseHandlerType.GetMethod("GetIndicator");
                    var genericMethod = method?.MakeGenericMethod(asset.Item3);
                    
                    //pass the parameters to the method
                    var result = (Indicator?) genericMethod?.Invoke(
                        databaseHandler, 
                        new object?[]
                        {
                            asset.Item2,
                            asset.Item1,
                            asset.Item3,
                            strategyParameter.TimeFrameStart,
                            strategyParameter.TimeFrameEnd
                        });

                    if (result is not null)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(
                        e,
                        "{Asset} | {TimeFrame} | {Indicator} | {LastClose} | could not get indicators from Database",
                        Enums.Assets.Btcusdt.GetStringValue(),
                        Enums.TimeFrames.M5.GetStringValue(),
                        asset.Item3.Name,
                        DateTime.Now.AddMonths(-1));

                    throw;
                }
            }

            return results;
        }
        
        private static string GetSelectedDllPaths(List<string> dllPaths)
        {
            return dllPaths[cursor];
        }
        
        /*
        private static void UpdateAppSettings(Dictionary<string, string> dllPaths)
        {
            var appSettingsPath = "appsettings.json";
            var json = File.ReadAllText(appSettingsPath);
            var jsonObj = JObject.Parse(json);

            var dllPathsSection = jsonObj.GetValue("DllPaths") as JObject;
            dllPathsSection.RemoveAll();

            foreach (var dllPath in dllPaths)
            {
                dllPathsSection.Add(dllPath.Key, dllPath.Value);
            }

            File.WriteAllText(appSettingsPath, jsonObj.ToString());
        }
        */
    }
}
