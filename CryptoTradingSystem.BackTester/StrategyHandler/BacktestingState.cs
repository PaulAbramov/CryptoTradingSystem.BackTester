using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Database.Models;
using Serilog;
using System;

namespace CryptoTradingSystem.BackTester.StrategyHandler;

internal class BacktestingState : IStrategyState
{
	public BacktestingState()
	{
	}

	// open "papertrade"
	public void OpenTrade(Asset entryCandle)
	{
		Log.Warning(
			"Open trade in backtesting state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			entryCandle.AssetName,
			entryCandle.CloseTime,
			entryCandle.CandleClose);
	}

	// close "papertrade"
	public void CloseTrade(StrategyHandler handler, Asset closeCandle)
	{
		Log.Warning(
			"Close trade in backtesting state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			closeCandle.AssetName,
			closeCandle.CloseTime,
			closeCandle.CandleClose);
	}
}