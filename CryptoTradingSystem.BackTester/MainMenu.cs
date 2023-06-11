using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTradingSystem.General.Helper;
using Microsoft.Extensions.Configuration;

namespace CryptoTradingSystem.BackTester;

public class MainMenu
{
    private int selectedOption;
    
    private readonly StrategiesManager strategiesManager;
    private readonly StrategiesExecutor strategiesExecutor;
    
    private static readonly List<string> MenuOptions = new()
    {
        "Handle strategies",
        "Execute selected strategies",
        "Stop strategies",
        "Exit"
    };

    private List<string> runningStrategiesTexts = new();

    public MainMenu(IConfiguration config)
    {
        strategiesManager = new StrategiesManager(config);
        strategiesExecutor = new StrategiesExecutor(config);
        strategiesExecutor.StrategyUpdateEvent += (sender, args) =>
        {
            runningStrategiesTexts = new List<string>();
            
            foreach (var statsString in from strategy in strategiesExecutor.RunningStrategies 
                     let statsString = $"{strategy.Name.Replace(".dll", string.Empty)} running... {strategy.TradesAmount} Trades made, currently at time: {strategy.CurrentCloseDateTime}" 
                     select statsString + (strategy.RunningTrade ? " | Running trade" : string.Empty))
            {
                runningStrategiesTexts.Add(statsString);
            }
            
            DrawMainMenu();
        };
    }
    
    public void StartMainMenu()
    {
        var exit = false;

        while (!exit)
        {
            DrawMainMenu();

            var keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    ConsoleHelper.HandleArrowKey(keyInfo.Key, MenuOptions, ref selectedOption);
                    break;

                case ConsoleKey.Enter:
                    if (selectedOption == MenuOptions.Count - 1)
                    {
                        // TODO end all Threads
                        // Exit the program
                        exit = true;
                    }
                    else
                    {
                        switch (selectedOption)
                        {
                            case 0:
                                strategiesManager.ManageStrategies();
                                break;
                            case 1:
                                strategiesExecutor.ExecuteSelectedStrategies();
                                break;
                            case 2:
                                // Zeige Menü von laufenden Strategien um die zu stoppen.
                                strategiesExecutor.StopStrategies();
                                break;
                        }
                    }
                    break;
            }
        }
    }

    private void DrawMainMenu()
    {
        Console.Clear();
        Console.WriteLine("Select an option:");

        for (var i = 0; i < MenuOptions.Count; i++)
        {
            Console.Write(selectedOption == i ? ">> " : "   ");

            Console.WriteLine(MenuOptions[i]);
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        foreach (var runningStrategy in runningStrategiesTexts)
        {
            Console.Write("   ");
            Console.WriteLine(runningStrategy);
        }

        Console.ResetColor();
    }
}