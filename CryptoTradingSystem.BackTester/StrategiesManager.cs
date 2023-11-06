using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Helper;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CryptoTradingSystem.BackTester;

public class StrategiesManager : IDisposable
{
	private readonly IConfiguration config;
	private readonly List<StrategyOption> defaultMenuOptions = new()
	{
		new() { Name = "Add Strategy" },
		new() { Name = "Remove selected Strategy" },
		new() { Name = "Back" }
	};

	private readonly List<string> runningStrategies = new();
	private readonly StrategiesExecutor strategiesExecutor;
	private int selectedOption;
	private List<StrategyOption>? strategies = new();

	public StrategiesManager(IConfiguration config, StrategiesExecutor strategiesExecutor)
	{
		this.config = config;
		this.strategiesExecutor = strategiesExecutor;
		this.strategiesExecutor.StrategyUpdateEvent += CheckStrategiesUpdates;
	}

	public void Dispose() => strategiesExecutor.StrategyUpdateEvent -= CheckStrategiesUpdates;

	public void ManageStrategies()
	{
		var exit = false;
		while (!exit)
		{
			DrawStrategiesMenu();

			var keyInfo = Console.ReadKey(true);
			exit = HandleKeyInput(keyInfo.Key);
		}
	}

	private void DrawStrategiesMenu()
	{
		CheckStrategiesUpdates(null, null);

		var originalForegroundColor = Console.ForegroundColor;

		Console.Clear();
		Console.WriteLine("Strategies can be marked in 3 states: gray, green, red");
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("Green marked strategies are activated.");
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(
			"Red marked strategies are marked to delete. Exception are running Strategies, end them first.");
		Console.ForegroundColor = originalForegroundColor;

		LoadStrategyDlls();

		for (var i = 0; i < strategies?.Count; i++)
		{
			Console.Write(selectedOption == i ? ">> " : "   ");

			Console.ForegroundColor = strategies[i].ActivityState switch
			{
				EStrategyActivityState.Enabled  => ConsoleColor.Green,
				EStrategyActivityState.ToDelete => ConsoleColor.Red,
				_                               => originalForegroundColor
			};

			Console.WriteLine($"{strategies[i].Name}");
			Console.ForegroundColor = originalForegroundColor;
		}
	}

	private void CheckStrategiesUpdates(object? sender, EventArgs? e)
	{
		runningStrategies.Clear();
		runningStrategies.AddRange(strategiesExecutor.RunningStrategies.Select(x => x.Name));
	}

	private bool HandleKeyInput(ConsoleKey key)
	{
		switch (key)
		{
			case ConsoleKey.UpArrow:
			case ConsoleKey.DownArrow:
				ConsoleHelper.HandleArrowKey(key, strategies?.ToList(), ref selectedOption);
				break;

			case ConsoleKey.Enter:
				if (selectedOption == strategies?.Count - 3)
				{
					AddStrategy(config);
					selectedOption = strategies!.Count - 3;
				}
				else if (selectedOption == strategies?.Count - 2)
				{
					DeleteMarkedStrategies(config);
					selectedOption = strategies!.Count - 2;
				}
				else if (selectedOption == strategies?.Count - 1)
				{
					// Exit the program
					return true;
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

		return false;
	}

	private void LoadStrategyDlls()
	{
		strategies?.Clear();
		strategies = SettingsHelper.GetStrategyOptions(config);

		Log.Debug(
			"Found following Strategies in appsettings: {strategies}",
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

		var filename = new FileInfo(path).Name;

		var strategiesInConfig = SettingsHelper.GetStrategyOptions(config);
		strategiesInConfig.Add(
			new()
			{
				Name = filename,
				Path = path,
				ActivityState = EStrategyActivityState.None
			});

		SettingsHelper.UpdateStrategyOptions(config, strategiesInConfig);

		Log.Debug(
			"Added Strategy: {Strategy} | {PathToStrategy}",
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

	private void ToggleStrategy(IConfiguration configToUpgrade, string strategyName)
	{
		var strategiesInConfig = SettingsHelper.GetStrategyOptions(configToUpgrade);
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

		// do not allow to set running strategies to be deleted
		if (strategy.ActivityState == EStrategyActivityState.ToDelete
		    && runningStrategies.Any(x => x == strategy.Name))
		{
			strategy.ActivityState = strategy.ActivityState.Next();
		}

		SettingsHelper.UpdateStrategyOptions(configToUpgrade, strategiesInConfig);

		Log.Debug(
			"Updated Strategy: {Strategy} | {PathToStrategy}",
			strategy.Name,
			strategy.Path);
	}
}