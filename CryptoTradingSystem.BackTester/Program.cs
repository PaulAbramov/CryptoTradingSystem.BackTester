using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester
{
    class Program
    {
        private static string StrategyDll = "StrategyDll";
        private static string ConnectionString = "ConnectionString";
        private static string LoggingLocation = "LoggingLocation";

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
            MethodInfo method;
            try
            {
                var DLLPath = new FileInfo(strategyDll);
                Assembly assembly = Assembly.LoadFile(DLLPath.FullName);
                Type t = assembly.GetTypes().First();
                obj = Activator.CreateInstance(t);
                method = t.GetMethod("ExecuteStrategy");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load strategy.dll. Please check the Path");
                return;
            }

            object[] parameters = new object[]
            {
                config.GetValue<string>(ConnectionString)
            };

            try
            {
                string TargetString = (string)method.Invoke(obj, parameters: parameters);
            }
            catch (Exception e)
            {
                Log.Error(e, "An Error appeared while executing the Strategy.");
            }

            Console.ReadKey();
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
