using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Helper;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CryptoTradingSystem.BackTester;

public class StrategiesExecutor
{
    private int selectedOption;

    private readonly IConfiguration config;
    private readonly Dictionary<string, CancellationTokenSource?> threads = new();

    public readonly List<RunningStrategy> RunningStrategies = new();
    
    // Declare the delegate (if using non-generic pattern).
    public delegate void StrategyUpdateEventHandler(object sender, EventArgs? e);

    // Declare the event.
    public event StrategyUpdateEventHandler? StrategyUpdateEvent;
    
    public StrategiesExecutor(IConfiguration config)
    {
        this.config = config;
    }
    
    /// <summary>
    /// Erstelle Threads pro Strategy, speichere die IDs um am Ende beim Schließen die Threads zu beenden.
    /// Wie stelle ich es am besten dar, dass ich noch aus dem Menü heraus beenden kann?
    /// 
    /// Drunter schreiben : {StrategieName} running... X Trades made, currently at time: {Closetime}?
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void ExecuteSelectedStrategies()
    {
        var connectionString = config.GetValue<string>(SettingsHelper.ConnectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Log.Error("No ConnectionString found in appsettings.json, please check the file");
            return;
        }

        var enabledStrategies = SettingsHelper.GetEnabledStrategyOptions(config);
        if (enabledStrategies.Count == 0)
        {
            return;
        }

        foreach (var strategy in enabledStrategies)
        {
            // Load methods "ExecuteStrategy" and "SetupStrategyParameter" from strategy.dll
            StrategyParameter strategyParameter;
            MethodInfo? executeStrategyMethod;
            object? obj;
            
            try
            {
                var dllPath = new FileInfo(strategy.Path);
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
                strategyParameter = (StrategyParameter)(method.Invoke(obj, null) ?? throw new InvalidOperationException());
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

            var newCancellationTokenSource = new CancellationTokenSource();

            var newStrategyThread = new Thread(() =>
            {
                ExecuteStrategy(newCancellationTokenSource.Token, 
                    connectionString,
                    strategyParameter, 
                    obj,
                    executeStrategyMethod);
            });

            threads.Add(strategy.Name, newCancellationTokenSource);
            
            // Start the threads
            newStrategyThread.Name = strategy.Name;
            newStrategyThread.Start();
            
            RunningStrategies.Add(new RunningStrategy
            {
                Name = strategy.Name
            });
            
            StrategyUpdateEvent?.Invoke(this, null);
        }
    }

    private void ExecuteStrategy(CancellationToken cancellationToken,
        string connectionString,
        StrategyParameter strategyParameter,
        object obj,
        MethodInfo executeStrategyMethod)
    {
        var tradestatus = Enums.TradeStatus.Closed;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
                
            var results = MySQLDatabaseHandler.GetDataFromDatabase(strategyParameter, connectionString);

            try
            {
                var strategy = RunningStrategies.FirstOrDefault(x => x.Name == Thread.CurrentThread.Name);
                if (strategy != null)
                {
                    strategy.CurrentCloseDateTime = results.Min(x => x.CloseTime);
                }
                
                var tradeType = (Enums.TradeType) executeStrategyMethod.Invoke(obj, new object?[]{results, tradestatus})!;
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

                        if (strategy != null)
                        {
                            strategy.RunningTrade = !strategy.RunningTrade;
                        }
                        
                        StrategyUpdateEvent?.Invoke(this, null);
                        break;
                    case Enums.TradeType.Sell:
                        Log.Warning("Sell {AssetName} at {CloseTime} | Price: {CandleClose}",
                            strategyParameter.AssetToBuy,
                            results[0].CloseTime,
                            results[0].Asset?.CandleClose);
                        tradestatus = Enums.TradeStatus.Closed;
                        
                        if (strategy != null)
                        {
                            strategy.TradesAmount++;
                            strategy.RunningTrade = !strategy.RunningTrade;
                        }
                        
                        StrategyUpdateEvent?.Invoke(this, null);
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
    
    public void StopStrategies()
    {
        var exit = false;
        threads.Add("Exit", null);

        while (!exit)
        {
            DrawRunningStrategies();

            var keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    ConsoleHelper.HandleArrowKey(keyInfo.Key, threads.Keys.ToList(), ref selectedOption);
                    break;

                case ConsoleKey.Enter:
                    if (selectedOption == threads.Count - 1)
                    {
                        // Exit the program
                        exit = true;
                        threads.Remove("Exit");
                    }
                    else
                    {
                        threads.ElementAt(selectedOption).Value?.Cancel();
                        //TODO  mit "waitone" auf das Ende des Threads warten und dann erst löschen
                        threads.Remove(threads.ElementAt(selectedOption).Key);
                        RunningStrategies.RemoveAll(x => x.Name == threads.ElementAt(selectedOption).Key);
                        
                        StrategyUpdateEvent?.Invoke(this, null);
                    }
                    break;
            }
        }
    }
    
    private void DrawRunningStrategies()
    {
        Console.Clear();
        Console.WriteLine("Select strategy to stop it:");

        // out of range index(?)
        for (var i = 0; i < threads.Count; i++)
        {
            Console.Write(selectedOption == i ? ">> " : "   ");

            // gegen null checken? falls ja, dann exit hinzufügen?
            Console.WriteLine(threads.ElementAt(i).Key);
        }
    }
}