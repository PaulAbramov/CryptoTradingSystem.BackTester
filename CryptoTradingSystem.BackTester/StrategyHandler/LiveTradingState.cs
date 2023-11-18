using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Database.Models;
using Serilog;
using System;
using System.Net.Http;

namespace CryptoTradingSystem.BackTester.StrategyHandler;

public class LiveTradingState : IStrategyState
{
	private HttpClient httpClient;

	public LiveTradingState()
	{
		httpClient = new HttpClient();
	}

	// open trade via API
	public void OpenTrade(StrategyHandler handler, Asset entryCandle)
	{
		Log.Warning(
			"Open trade in live-trading state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			entryCandle.AssetName,
			entryCandle.CloseTime,
			entryCandle.CandleClose);
	}

	// close trade via API
	// see if statistics minimals are still reached
	//	if not, go back and restart validationphase
	public void CloseTrade(StrategyHandler handler, Asset closeCandle)
	{
		Log.Warning(
			"Close trade in live-trading state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			closeCandle.AssetName,
			closeCandle.CloseTime,
			closeCandle.CandleClose);

		if (closeCandle.CloseTime <= DateTime.Today.AddHours(23))
		{
			return;
		}

		// Get statistics here

		var statisticsReached = true;
		if (statisticsReached)
		{
			handler.SetState(new ValidationState());
		}
	}
}