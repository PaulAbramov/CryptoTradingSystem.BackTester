using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CryptoTradingSystem.BackTester;

public static class SettingsHelper
{
    public static List<StrategyOption>? GetStrategyOptions(IConfiguration config)
    {
        var dllPathsSection = config.GetSection("StrategyDlls");
        var strategiesInConfig = new List<StrategyOption>();
        if (dllPathsSection.Value != null)
        {
            strategiesInConfig = JsonSerializer.Deserialize<List<StrategyOption>>(dllPathsSection.Value);
        }

        return strategiesInConfig;
    }
    
    public static void UpdateAppSettings(IConfiguration config, List<StrategyOption>? strategiesInConfig)
    {
        var jsonWriteOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        
        var strategiesJson = JsonSerializer.Serialize(strategiesInConfig, jsonWriteOptions);
        config["StrategyDlls"] = strategiesJson;

        var configAsDict = config
            .AsEnumerable()
            .ToDictionary(c => c.Key, c => c.Value);
        var json = JsonSerializer.Serialize(configAsDict, jsonWriteOptions);

        File.WriteAllText("appsettings.json", json);
    }
}