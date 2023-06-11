using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Helper;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester;

public class StrategiesManager
{
    private int selectedOption;
    private List<StrategyOption>? strategies = new();
    
    private readonly IConfiguration config;
    private readonly List<StrategyOption> defaultMenuOptions = new()
    {
        new StrategyOption { Name = "Add Strategy" },
        new StrategyOption { Name = "Remove selected Strategy" },
        new StrategyOption { Name = "Back" }
    };

    public StrategiesManager(IConfiguration config)
    {
        this.config = config;
    }

    public void ManageStrategies()
    {
        var exit = false;
        while (!exit)
        {
            DrawStrategiesMenu();
            
            var keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    ConsoleHelper.HandleArrowKey(keyInfo.Key, strategies?.ToList(), ref selectedOption);
                    break;

                case ConsoleKey.Enter:
                    if (selectedOption == strategies?.Count - 3)
                    {
                        AddStrategy(config);
                        selectedOption = strategies!.Count - 3;
                    }
                    else if (selectedOption == strategies?.Count - 2)
                    {
                        //TODO make sure you cant delete running Strategy
                        DeleteMarkedStrategies(config);
                        selectedOption = strategies!.Count - 2;
                    }
                    else if (selectedOption == strategies?.Count - 1)
                    {
                        // Exit the program
                        exit = true;
                    }
                    else
                    {
                        if (strategies != null)
                        {
                            ToggleStrategy(config, strategies[selectedOption].Name);
                        }
                    }
                    break;
            }
        }
    }
    
    private void DrawStrategiesMenu()
    {
        var originalForegroundColor = Console.ForegroundColor;

        Console.Clear();
        Console.WriteLine("Strategies can be marked in 3 states: gray, green, red");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Green marked strategies are activated.");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Red marked strategies are marked to delete.");
        Console.ForegroundColor = originalForegroundColor;

        LoadStrategyDlls();

        for (var i = 0; i < strategies?.Count; i++)
        {
            Console.Write(selectedOption == i ? ">> " : "   ");

            Console.ForegroundColor = strategies[i].ActivityState switch
            {
                EStrategyActivityState.Enabled => ConsoleColor.Green,
                EStrategyActivityState.ToDelete => ConsoleColor.Red,
                _ => originalForegroundColor
            };
            
            Console.WriteLine($"{strategies[i].Name}");
            Console.ForegroundColor = originalForegroundColor;
        }
    }
    
    private void LoadStrategyDlls()
    {
        strategies?.Clear();
        strategies = SettingsHelper.GetStrategyOptions(config);
        
        Log.Debug("Found following Strategies in appsettings: {strategies}",
                string.Join(", ", strategies.Select(x => x.Name)));
        
        strategies?.AddRange(defaultMenuOptions);
    }

    private static void AddStrategy(IConfiguration config)
    {
        Log.Information("Pass the absolute path to the .dll file:");
        var path = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(path)
            || !path.EndsWith(".dll"))
        {
            return;
        }

        var filename = (new FileInfo(path)).Name;

        var strategiesInConfig = SettingsHelper.GetStrategyOptions(config);
        strategiesInConfig.Add(new StrategyOption { Name = filename, Path = path, ActivityState = EStrategyActivityState.None });

        SettingsHelper.UpdateStrategyOptions(config, strategiesInConfig);
        
        Log.Debug("Added Strategy: {Strategy} | {PathToStrategy}", 
            filename, 
            path);
    }

    private static void DeleteMarkedStrategies(IConfiguration config)
    {
        var strategiesInConfig = SettingsHelper.GetStrategyOptions(config);
        if (strategiesInConfig.Count == 0)
        {
            return;
        }
        
        strategiesInConfig.RemoveAll(x => x.ActivityState == EStrategyActivityState.ToDelete);
        SettingsHelper.UpdateStrategyOptions(config, strategiesInConfig);
    }

    private static void ToggleStrategy(IConfiguration config, string strategyName)
    {
        var strategiesInConfig = SettingsHelper.GetStrategyOptions(config);

        if (strategiesInConfig.Count == 0)
        {
            return;
        }

        var strategy = strategiesInConfig.FirstOrDefault(x => x.Name == strategyName);
        if (strategy == null)
        {
            return;
        }

        strategy.ActivityState = strategy.ActivityState.Next();

        SettingsHelper.UpdateStrategyOptions(config, strategiesInConfig);

        Log.Debug("Updated Strategy: {Strategy} | {PathToStrategy}",
            strategy.Name,
            strategy.Path);
    }
}