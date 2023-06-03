using System;
using System.Collections.Generic;
using CryptoTradingSystem.General.Helper;
using Microsoft.Extensions.Configuration;

namespace CryptoTradingSystem.BackTester;

public class MainMenu
{
    private int selectedOption;
    
    private static readonly List<string> MenuOptions = new List<string>()
    {
        "Handle strategies",
        "Execute selected strategies",
        "Exit"
    };
    
    public void StartMainMenu(IConfiguration config)
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
                        // Exit the program
                        exit = true;
                    }
                    else
                    {
                        switch (selectedOption)
                        {
                            case 0:
                                var strategiesManager = new StrategiesManager();
                                strategiesManager.ManageStrategies(config);
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
    }
}