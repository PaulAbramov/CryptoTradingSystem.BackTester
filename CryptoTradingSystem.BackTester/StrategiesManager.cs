using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CryptoTradingSystem.General.Helper;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester;

public class StrategiesManager
{
    private int selectedOption;
    private List<StrategyOption>? strategies = new();
    private readonly List<StrategyOption> defaultMenuOptions = new()
    {
        new StrategyOption { Name = "Add Strategy" },
        new StrategyOption { Name = "Remove selected Strategy" },
        new StrategyOption { Name = "Back" }
    };

    public void ManageStrategies(IConfiguration config)
    {
        bool exit = false;
        while (!exit)
        {
            DrawStrategiesMenu(config);
            
            var keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    ConsoleHelper.HandleArrowKey(keyInfo.Key, strategies?.ToList(), ref selectedOption);
                    break;

                case ConsoleKey.Enter:
                    if (selectedOption == strategies?.Count - 1)
                    {
                        // Exit the program
                        exit = true;
                    }
                    else
                    {
                        Log.Debug($"{strategies?[selectedOption].Name} selected");
                        if (selectedOption < strategies?.Count - 3)
                        {
                            if (strategies != null)
                            {
                                strategies[selectedOption].Enabled = true;
                                ToggleStrategy(config, strategies[selectedOption].Name);
                            }
                        }
                        else if (selectedOption == strategies?.Count - 3)
                        {
                            AddStrategy(config);
                        }
                    }
                    break;
            }
        }
    }
    
    private void DrawStrategiesMenu(IConfiguration config)
    {
        Console.Clear();
        GetStrategyDlls(config);

        var originalForegroundColor = Console.ForegroundColor;
        for (var i = 0; i < strategies?.Count; i++)
        {
            Console.Write(selectedOption == i ? ">> " : "   ");

            if (strategies[i].Enabled)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            
            Console.WriteLine($"{strategies[i].Name}");

            if (strategies[i].Enabled)
            {
                Console.ForegroundColor = originalForegroundColor;
            }
        }
    }
    
    private void GetStrategyDlls(IConfiguration config)
    {
        strategies?.Clear();
        var dllPathsSection = config.GetSection("StrategyDlls");
        if (dllPathsSection.Value != null)
        {
            strategies = JsonSerializer.Deserialize<List<StrategyOption>>(dllPathsSection.Value);
        }
        if (strategies == null)
        {
            strategies = new List<StrategyOption>();
        }
        
        Log.Debug("Found following Strategies in appsettings: {strategies}",
                string.Join(", ", strategies.Select(x => x.Name)));
        
        strategies?.AddRange(defaultMenuOptions);
    }

    private static void AddStrategy(IConfiguration config)
    {
        Log.Information("Pass the absolute path to the .dll file:");
        var path = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var filename = (new FileInfo(path)).Name;

        
        var strategiesInConfig = SettingsHelper.GetStrategyOptions(config);
        
        if (strategiesInConfig == null ||
            strategiesInConfig.Count == 0)
        {
            return;
        }
        strategiesInConfig?.Add(new StrategyOption { Name = filename, Path = path, Enabled = false});

        SettingsHelper.UpdateStrategyOptions(config, strategiesInConfig);
        
        Log.Debug("Added Strategy: {Strategy} | {PathToStrategy}", 
            filename, 
            path);
    }

    private static void ToggleStrategy(IConfiguration config, string strategyName)
    {
        var strategiesInConfig = SettingsHelper.GetStrategyOptions(config);
        
        if (strategiesInConfig == null ||
            strategiesInConfig.Count == 0)
        {
            return;
        }

        var strategy = strategiesInConfig.FirstOrDefault(x => x.Name == strategyName);
        if (strategy == null)
        {
            return;
        }
        
        strategy.Enabled = !strategy.Enabled;

        SettingsHelper.UpdateStrategyOptions(config, strategiesInConfig);
        
        Log.Debug("Updated Strategy: {Strategy} | {PathToStrategy}", 
            strategy.Name, 
            strategy.Path);
    }
}