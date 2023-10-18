using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Helper;
using CryptoTradingSystem.General.Strategy;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoTradingSystem.BackTester;

public class StrategiesExecutor : IDisposable
{
    private const string exitString = "Exit";

    private int selectedOption;

    private readonly IConfiguration config;
    private readonly SortedDictionary<Thread, CancellationTokenSource?> threads = new();

    public readonly List<RunningStrategy> RunningStrategies = new();

    public delegate void StrategyUpdateEventHandler(object sender, EventArgs? e);
    public event StrategyUpdateEventHandler? StrategyUpdateEvent;

    public StrategiesExecutor(IConfiguration config)
    {
        this.config = config;
    }

    public void Dispose()
    {
        foreach (var runningThread in threads)
        {
            runningThread.Value?.Cancel();
        }
    }

    /// <summary>
    /// Go through all enabled strategies and execute them
    /// First get the strategyparameter, the executionmethod and the object from the strategy.dll
    /// Then execute the strategy in a new thread
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

        foreach (var strategy in enabledStrategies.Where(strategy => RunningStrategies.All(x => x.Name != strategy.Name)))
        {
            var executionParameter = GetExecutionParameter(strategy.Path);
            if (executionParameter is null)
            {
                continue;
            }

            if (executionParameter?.Item1?.Assets.Count == 0)
            {
                Log.Error("No Assets requested in Strategyparameter");
                continue;
            }

            StartStrategy(strategy.Name, connectionString, executionParameter!);
        }
    }

    /// <summary>
    /// Load methods "ExecuteStrategy" and "SetupStrategyParameter" from strategy.dll
    /// </summary>
    /// <param name="strategyPath"></param>
    /// <returns></returns>
    private Tuple<StrategyParameter?, object?, MethodInfo?>? GetExecutionParameter(string strategyPath)
    {
        try
        {
            var dllPath = new FileInfo(strategyPath);
            var assembly = Assembly.LoadFile(dllPath.FullName);
            var types = assembly.GetTypes();
            var t = types.FirstOrDefault(x => x.Name is "Strategy");
            if (t is null)
            {
                Log.Error("Could not load strategy.dll. Please check the Path");
                return null;
            }

            var obj = Activator.CreateInstance(t) ?? throw new InvalidOperationException();

            var method = t.GetMethod("SetupStrategyParameter");
            var executeStrategyMethod = t.GetMethod("ExecuteStrategy");
            if (executeStrategyMethod is null || method is null)
            {
                Log.Error("Could not load methods 'SetupStrategyParameter' and 'ExecuteStrategy' from strategy.dll. " +
                          "Please check the Strategy");
                return null;
            }

            // execute and return SetupStrategyParameter
            return new Tuple
                <StrategyParameter?,
                object?,
                MethodInfo?>(
                    (StrategyParameter)(method.Invoke(obj, null) ?? throw new InvalidOperationException()),
                    obj,
                    executeStrategyMethod
                );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not load strategy.dll. Please check the Path");
            return null;
        }
    }

    /// <summary>
    /// Execute the strategy in a new thread
    /// Safe the thread and the CancellationTokenSource to be able to stop the thread
    /// </summary>
    /// <param name="strategyName"></param>
    /// <param name="connectionString"></param>
    /// <param name="executionParameter"></param>
    private void StartStrategy(string strategyName, string connectionString, Tuple<StrategyParameter?, object?, MethodInfo?> executionParameter)
    {
        if (executionParameter.Item1 is null
            || executionParameter.Item2 is null
            || executionParameter.Item3 is null)
        {
            return;
        }

        var newCancellationTokenSource = new CancellationTokenSource();
        var newStrategyThread = new Thread(() =>
        {
            ExecuteStrategy(newCancellationTokenSource.Token,
                connectionString,
                (StrategyParameter)executionParameter?.Item1!,
                executionParameter?.Item2,
                executionParameter?.Item3);
        });

        // Start the threads
        newStrategyThread.Name = strategyName;
        newStrategyThread.Start();

        threads.Add(newStrategyThread, newCancellationTokenSource);

        RunningStrategies.Add(new RunningStrategy
        {
            Name = strategyName
        });

        StrategyUpdateEvent?.Invoke(this, null);
    }

    private void ExecuteStrategy(CancellationToken cancellationToken,
        string connectionString,
        StrategyParameter strategyParameter,
        object? obj,
        MethodInfo? executeStrategyMethod)
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

                var returnParameter = (StrategyReturnParameter)executeStrategyMethod!.Invoke(obj, new object?[] { results, tradestatus })!;
                switch (returnParameter.TradeStatus)
                {
                    case Enums.TradeStatus.Open:
                        tradestatus = Enums.TradeStatus.Open;

                        switch (returnParameter.TradeType)
                        {
                            case Enums.TradeType.None:
                                break;
                            case Enums.TradeType.Buy:
                                Log.Warning("Buy {AssetName} at {CloseTime}| Price: {CandleClose}",
                                    strategyParameter.AssetToBuy,
                                    results[0].CloseTime,
                                    results[0].Asset?.CandleClose);
                                break;
                            case Enums.TradeType.Sell:
                                Log.Warning("Sell {AssetName} at {CloseTime} | Price: {CandleClose}",
                                    strategyParameter.AssetToBuy,
                                    results[0].CloseTime,
                                    results[0].Asset?.CandleClose);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (strategy != null)
                        {
                            strategy.RunningTrade = !strategy.RunningTrade;
                            strategy.TradeOpenPrice = results[0].Asset?.CandleClose ?? 0;
                        }

                        StrategyUpdateEvent?.Invoke(this, null);

                        break;
                    case Enums.TradeStatus.Closed:
                        tradestatus = Enums.TradeStatus.Closed;

                        switch (returnParameter.TradeType)
                        {
                            case Enums.TradeType.None:
                                break;
                            case Enums.TradeType.Buy:
                                Log.Warning("Buy {AssetName} at {CloseTime}| Price: {CandleClose}",
                                    strategyParameter.AssetToBuy,
                                    results[0].CloseTime,
                                    results[0].Asset?.CandleClose);

                                SetStatisticsClosedTrades(strategy, results[0].Asset?.CandleClose, returnParameter.TradeType);
                                break;
                            case Enums.TradeType.Sell:
                                Log.Warning("Sell {AssetName} at {CloseTime} | Price: {CandleClose}",
                                    strategyParameter.AssetToBuy,
                                    results[0].CloseTime,
                                    results[0].Asset?.CandleClose);

                                SetStatisticsClosedTrades(strategy, results[0].Asset?.CandleClose, returnParameter.TradeType);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        StrategyUpdateEvent?.Invoke(this, null);

                        break;
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

    public async Task StopStrategiesMenu()
    {
        var exit = false;

        if (threads.All(x => x.Key.Name != exitString))
        {
            // Add exit option
            threads.Add(new Thread(() => { }) { Name = exitString }, null);
        }

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
                    var savedThread = threads.ElementAt(selectedOption).Key;
                    if (savedThread.Name == exitString)
                    {
                        // Exit the program
                        threads.Remove(savedThread);
                        exit = true;
                    }
                    else
                    {
                        await StopStrategy(savedThread);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Remove the thread from the lists and stop it
    /// </summary>
    /// <returns></returns>
    private async Task StopStrategy(Thread savedThread)
    {
        RunningStrategies.RemoveAll(x => x.Name == savedThread.Name);

        threads.ElementAt(selectedOption).Value?.Cancel();

        while (savedThread.IsAlive)
        {
            await Task.Delay(200);
        }
        //TODO  mit "waitone" auf das Ende des Threads warten und dann erst löschen
        threads.Remove(threads.ElementAt(selectedOption).Key);

        StrategyUpdateEvent?.Invoke(this, null);
    }

    private void DrawRunningStrategies()
    {
        Console.Clear();
        Console.WriteLine("Select strategy to stop it:");

        for (var i = 0; i < threads.Count; i++)
        {
            Console.Write(selectedOption == i ? ">> " : "   ");
            Console.WriteLine(threads.ElementAt(i).Key);
        }
    }

    /// <summary>
    /// Set stastics after closing the trade
    /// </summary>
    /// <param name="strategy"></param>
    /// <param name="candleClose"></param>
    /// <param name="tradeType"></param>
    private void SetStatisticsClosedTrades(RunningStrategy? strategy, decimal? candleClose, Enums.TradeType tradeType)
    {
        if (candleClose != null
            && strategy != null)
        {
            decimal? profitLoss = 0;

            if (tradeType == Enums.TradeType.Buy)
            {
                profitLoss = strategy.TradeOpenPrice - candleClose;
            }
            else if (tradeType == Enums.TradeType.Sell)
            {
                profitLoss = candleClose - strategy.TradeOpenPrice;
            }

            _ = profitLoss > 0 ? strategy.StrategyAnalytics.AmountOfWonTrades++ : strategy.StrategyAnalytics.AmountOfLostTrades++;
            strategy.StrategyAnalytics.Profit += profitLoss.Value;

            strategy.StrategyAnalytics.TradesAmount++;
            strategy.RunningTrade = !strategy.RunningTrade;
        }
    }
}