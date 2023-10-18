using CryptoTradingSystem.General.Helper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoTradingSystem.BackTester;

public class MainMenu : IDisposable
{
    private int selectedOption;
    private List<string> runningStrategiesTexts = new();

    private readonly StrategiesManager strategiesManager;
    private readonly StrategiesExecutor strategiesExecutor;

    private static readonly List<string> MenuOptions = new()
    {
        "Handle strategies",
        "Execute selected strategies",
        "Stop strategies",
        "Exit"
    };

    public MainMenu(IConfiguration config)
    {
        strategiesExecutor = new StrategiesExecutor(config);
        strategiesManager = new StrategiesManager(config, strategiesExecutor);
        strategiesExecutor.StrategyUpdateEvent += CheckStrategiesUpdates;
    }

    public void Dispose()
    {
        strategiesExecutor.StrategyUpdateEvent -= CheckStrategiesUpdates;
        strategiesManager.Dispose();
        strategiesExecutor.Dispose();
    }

    private void CheckStrategiesUpdates(object sender, EventArgs? e)
    {
        runningStrategiesTexts = new List<string>();

        foreach (var statsString in from strategy in strategiesExecutor.RunningStrategies
                                    let statsString = $"{strategy.Name.Replace(".dll", string.Empty)} running... {strategy.StrategyAnalytics.TradesAmount} Trades made, currently at time: {strategy.CurrentCloseDateTime}"
                                    select statsString + (strategy.RunningTrade ? " | Running trade" : string.Empty))
        {
            runningStrategiesTexts.Add(statsString);
        }
    }

    public void StartMainMenu()
    {
        var exit = false;

        while (!exit)
        {
            DrawMainMenu();

            var keyInfo = Console.ReadKey(true);

            exit = HandleKeyInput(keyInfo.Key);
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

    private bool HandleKeyInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                ConsoleHelper.HandleArrowKey(key, MenuOptions, ref selectedOption);
                break;

            case ConsoleKey.Enter:
                return HandleEnterKey();
        }

        return false;
    }

    private bool HandleEnterKey()
    {
        if (selectedOption == MenuOptions.Count - 1)
        {
            // Exit the program
            return true;
        }
        else
        {
            HandleStrategyMenus();
        }

        return false;
    }

    private void HandleStrategyMenus()
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
                // Show menu of running strategies to be able to stop chosen ones
                strategiesExecutor.StopStrategiesMenu().GetAwaiter().GetResult();
                break;
        }
    }
}