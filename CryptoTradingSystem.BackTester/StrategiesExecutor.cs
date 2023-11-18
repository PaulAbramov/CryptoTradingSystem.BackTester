using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
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
	public delegate void StrategyUpdateEventHandler(object sender, EventArgs? e);
	public event StrategyUpdateEventHandler? StrategyUpdateEvent;

	public readonly List<StrategyHandler.StrategyHandler> StrategyHandlers = new();

	private const string exitString = "Exit";

	private readonly IConfiguration? config;
	private readonly SortedDictionary<Thread, CancellationTokenSource?> threads = new();

	private int selectedOption;

	public StrategiesExecutor() { }

	public StrategiesExecutor(IConfiguration config) => this.config = config;

	public void Dispose()
	{
		foreach (var runningThread in threads)
		{
			runningThread.Value?.Cancel();
		}
	}

	/// <summary>
	///   Go through all enabled strategies and execute them
	///   First get the strategyparameter, the executionmethod and the object from the strategy.dll
	///   Then execute the strategy in a new thread
	/// </summary>
	/// <exception cref="InvalidOperationException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public void ExecuteSelectedStrategies()
	{
		if (config is null)
		{
			return;
		}

		var connectionString = config?.GetValue<string>(SettingsHelper.ConnectionString);
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			Log.Error("No ConnectionString found in appsettings.json, please check the file");
			return;
		}

		var enabledStrategies = SettingsHelper.GetEnabledStrategyOptions(config!);
		if (enabledStrategies.Count == 0)
		{
			return;
		}

		foreach (var strategy in enabledStrategies.Where(
			         strategy => StrategyHandlers.All(x => x.Name != strategy.Name)))
		{
			SetupStrategyExecution(strategy, connectionString);
		}
	}

	public async Task StopStrategiesMenu()
	{
		var exit = false;

		if (threads.All(x => x.Key.Name != exitString))
		{
			// Add exit option
			threads.Add(new(() => { }) { Name = exitString }, null);
		}

		while (!exit)
		{
			DrawRunningStrategies();

			var keyInfo = Console.ReadKey(true);
			exit = await HandleKeyInput(keyInfo.Key);
		}
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

	private async Task<bool> HandleKeyInput(ConsoleKey key)
	{
		switch (key)
		{
			case ConsoleKey.UpArrow:
			case ConsoleKey.DownArrow:
				ConsoleHelper.HandleArrowKey(key, threads.Keys.ToList(), ref selectedOption);
				break;

			case ConsoleKey.Enter:
				await HandleEnterKey();
				break;
		}

		return false;
	}

	private async Task<bool> HandleEnterKey()
	{
		var savedThread = threads.ElementAt(selectedOption).Key;
		if (savedThread.Name == exitString)
		{
			// Exit the program
			threads.Remove(savedThread);
			return true;
		}

		await StopStrategy(savedThread);

		return false;
	}

	/// <summary>
	///   Remove the thread from the lists and stop it
	/// </summary>
	/// <returns></returns>
	private async Task StopStrategy(Thread savedThread)
	{
		StrategyHandlers.RemoveAll(x => x.Name == savedThread.Name);

		threads.ElementAt(selectedOption).Value?.Cancel();

		while (savedThread.IsAlive)
		{
			await Task.Delay(200);
		}

		//TODO  mit "waitone" auf das Ende des Threads warten und dann erst löschen
		threads.Remove(threads.ElementAt(selectedOption).Key);

		StrategyUpdateEvent?.Invoke(this, null);
	}

	private void SetupStrategyExecution(StrategyOption strategy, string connectionString)
	{
		var executionParameter = GetExecutionParameter(strategy.Path);
		if (executionParameter is null)
		{
			return;
		}

		if (executionParameter.Item1?.Assets.Count == 0)
		{
			Log.Error("No Assets requested in Strategyparameter");
			return;
		}

		StartStrategy(strategy.Name, connectionString, executionParameter);
	}

	/// <summary>
	///   Load methods "ExecuteStrategy" and "SetupStrategyParameter" from strategy.dll
	/// </summary>
	/// <param name="strategyPath"></param>
	/// <returns></returns>
	private static Tuple<StrategyParameter?, object?, MethodInfo?>? GetExecutionParameter(string strategyPath)
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
				Log.Error(
					"Could not load methods 'SetupStrategyParameter' and 'ExecuteStrategy' from strategy.dll. "
					+ "Please check the Strategy");
				return null;
			}

			// execute and return SetupStrategyParameter
			return new(
				(StrategyParameter) (method.Invoke(obj, null) ?? throw new InvalidOperationException()),
				obj,
				executeStrategyMethod);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Could not load strategy.dll. Please check the Path");
			return null;
		}
	}

	/// <summary>
	///   Execute the strategy in a new thread
	///   Safe the thread and the CancellationTokenSource to be able to stop the thread
	/// </summary>
	/// <param name="strategyName"></param>
	/// <param name="connectionString"></param>
	/// <param name="executionParameter"></param>
	private void StartStrategy(
		string strategyName,
		string connectionString,
		Tuple<StrategyParameter?, object?, MethodInfo?> executionParameter)
	{
		if (executionParameter.Item1 is null
		    || executionParameter.Item2 is null
		    || executionParameter.Item3 is null)
		{
			return;
		}

		var newCancellationTokenSource = new CancellationTokenSource();

		// ReSharper disable once UseObjectOrCollectionInitializer
		var newStrategyThread = new Thread(
			() =>
			{
				ExecuteStrategy(
					connectionString,
					(StrategyParameter) executionParameter.Item1!,
					executionParameter.Item2,
					executionParameter.Item3,
					newCancellationTokenSource.Token);
			});

		// Start the threads
		newStrategyThread.Name = strategyName;
		newStrategyThread.Start();

		threads.Add(newStrategyThread, newCancellationTokenSource);

		// TODO pass initialInvestment to the StrategyHandler from strategyparameter
		StrategyHandlers.Add(new(strategyName, 1000));

		StrategyUpdateEvent?.Invoke(this, null);
	}

	private void ExecuteStrategy(
		string connectionString,
		StrategyParameter strategyParameter,
		object? obj,
		MethodInfo? executeStrategyMethod,
		CancellationToken cancellationToken)
	{
		var tradestatus = Enums.TradeStatus.Closed;
		while (true)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			var results = MySQLDatabaseHandler.GetDataFromDatabase(strategyParameter, connectionString);
			if (results.Count == 0)
			{
				continue;
			}

			try
			{
				HandleData(results, strategyParameter, obj, executeStrategyMethod, tradestatus);
			}
			catch (Exception e)
			{
				Log.Error(e, "An Error appeared while executing the Strategy");
				return;
			}
		}
	}

	private void HandleData(
		IReadOnlyList<Indicator?> results,
		StrategyParameter strategyParameter,
		object? obj,
		MethodInfo? executeStrategyMethod,
		Enums.TradeStatus tradestatus)
	{
		// TODO check if tradestatus and strategystate are persisted
		var strategy = StrategyHandlers.FirstOrDefault(x => x.Name == Thread.CurrentThread.Name);
		if (strategy == null)
		{
			return;
		}

		strategy.CurrentCloseDateTime = results.Min(x => x!.CloseTime);

		var returnParameter = (StrategyReturnParameter) executeStrategyMethod!.Invoke(
			obj,
			new object?[]
			{
				results,
				tradestatus
			})!;
		
		tradestatus = returnParameter.TradeStatus;
		switch (returnParameter.TradeStatus)
		{
			case Enums.TradeStatus.Open:
				HandleOpenTrade(
					strategy,
					returnParameter.TradeType,
					results[0]!.Asset!);
				break;
			case Enums.TradeStatus.Closed:
				HandleCloseTrade(
					strategy,
					returnParameter.TradeType,
					results[0]!.Asset!);
				break;
		}

		strategyParameter.TimeFrameStart = results.Min(x => x!.CloseTime);
	}

	private void HandleOpenTrade(
		StrategyHandler.StrategyHandler strategy,
		Enums.TradeType tradeType,
		Asset candle)
	{
		strategy.OpenTrade(tradeType, candle);

		// TODO need I to separate between Buy and Sell order?
		// switch (tradeType)
		// {
		// 	case Enums.TradeType.None:
		// 		break;
		// 	case Enums.TradeType.Buy:
		// 		Log.Warning(
		// 			"Long {AssetName} at {CloseTime}| Price: {CandleClose}",
		// 			assetToBuy,
		// 			closeTime,
		// 			candleClose);
		// 		break;
		// 	case Enums.TradeType.Sell:
		// 		Log.Warning(
		// 			"Short {AssetName} at {CloseTime} | Price: {CandleClose}",
		// 			assetToBuy,
		// 			closeTime,
		// 			candleClose);
		// 		break;
		// 	default:
		// 		throw new ArgumentOutOfRangeException();
		// }

		strategy.RunningTrade = !strategy.RunningTrade;

		StrategyUpdateEvent?.Invoke(this, null);
	}

	private void HandleCloseTrade(
		StrategyHandler.StrategyHandler strategy,
		Enums.TradeType tradeType,
		Asset candle)
	{
		strategy.CloseTrade(tradeType, candle);

		// TODO need I to separate between Buy and Sell order?
		// switch (tradeType)
		// {
		// 	case Enums.TradeType.None:
		// 		break;
		// 	case Enums.TradeType.Buy:
		// 		
		// 		break;
		// 	case Enums.TradeType.Sell:
		// 		Log.Warning(
		// 			"Close Long for {AssetName} at {CloseTime} | Price: {CandleClose}",
		// 			assetToBuy,
		// 			candle.CloseTime,
		// 			candle.CandleClose);
		//
		// 		break;
		// 	default:
		// 		throw new ArgumentOutOfRangeException();
		// }

		strategy.RunningTrade = !strategy.RunningTrade;
		strategy.TradesAmount++;

		StrategyUpdateEvent?.Invoke(this, null);
	}
}