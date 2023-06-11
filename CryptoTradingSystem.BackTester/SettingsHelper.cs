using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CryptoTradingSystem.BackTester;

public static class SettingsHelper
{
    private const string strategySection = "StrategyDlls";
    public const string AppsettingsFile = "appsettings.json";
    public const string LoggingLocation = "LoggingLocation";
    public const string ConnectionString = "ConnectionString";

    public static List<StrategyOption> GetEnabledStrategyOptions(IConfiguration config)
    {
        return GetStrategyOptions(config).Where(x => x.ActivityState == EStrategyActivityState.Enabled).ToList();
    }
    
    public static List<StrategyOption> GetStrategyOptions(IConfiguration config)
    {
        var dllPathsSection = config.GetSection(strategySection);
        var strategiesInConfig = new List<StrategyOption>();
        if (dllPathsSection.Value != null)
        {
            strategiesInConfig = JsonSerializer.Deserialize<List<StrategyOption>>(dllPathsSection.Value);
        }

        return strategiesInConfig!;
    }
    
    public static void UpdateStrategyOptions(IConfiguration config, List<StrategyOption>? strategiesInConfig)
    {
        var jsonWriteOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        
        var strategiesJson = JsonSerializer.Serialize(strategiesInConfig, jsonWriteOptions);
        config[strategySection] = strategiesJson;

        var configAsDict = config
            .AsEnumerable()
            .ToDictionary(c => c.Key, c => c.Value);
        var json = JsonSerializer.Serialize(configAsDict, jsonWriteOptions);

        File.WriteAllText(AppsettingsFile, json);
    }
}