using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Database.Models;
using Serilog;
using System;

namespace CryptoTradingSystem.BackTester.StrategyHandler;

public class BacktestingState : IStrategyState
{
	public BacktestingState()
	{
	}

	// open "papertrade"
	public void OpenTrade(StrategyHandler handler, Asset entryCandle)
	{
		Log.Warning(
			"Open trade in backtesting state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			entryCandle.AssetName,
			entryCandle.CloseTime,
			entryCandle.CandleClose);
	}

	// close "papertrade"
	// check if candle is the most recent one, by getting timeframe and check against now
	//	if so, switch into validating
	public void CloseTrade(StrategyHandler handler, Asset closeCandle)
	{
		Log.Warning(
			"Close trade in backtesting state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			closeCandle.AssetName,
			closeCandle.CloseTime,
			closeCandle.CandleClose);
		
		// If we are not at the end of the day, we can't validate yet
		if (closeCandle.CloseTime > DateTime.Today.AddHours(23))
		{
			handler.SetState(new ValidationState());
		}
	}
}