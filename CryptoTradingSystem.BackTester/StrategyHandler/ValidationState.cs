using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Database.Models;
using Serilog;
using System;

namespace CryptoTradingSystem.BackTester.StrategyHandler;

public class ValidationState : StrategyState
{
	private readonly DateTime startOfApprovementDuration;
	
	public ValidationState()
	{
		startOfApprovementDuration = DateTime.Now;
	}
	
	// open "papertrade" - "forwardtrade"
	public override void OpenTrade(StrategyHandler handler, Asset entryCandle)
	{
		Log.Warning(
			"Open trade in validation state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			entryCandle.AssetName,
			entryCandle.CloseTime,
			entryCandle.CandleClose);
	}

	// close "papertrade"
	// check if strategy is still in approvementDuration
	//	if not, see if statistics minimals are reached
	//		if so, switch into live trading
	public override void CloseTrade(StrategyHandler handler, Asset closeCandle)
	{
		Log.Warning(
			"Close trade in validation state for {AssetName} at {CloseTime}| Price: {CandleClose}",
			closeCandle.AssetName,
			closeCandle.CloseTime,
			closeCandle.CandleClose);
		
		if (startOfApprovementDuration.Add(handler.ApprovementDuration) >= DateTime.Now)
		{
			return;
		}

		// Get statistics here
		
		var statisticsReached = true;
		if (statisticsReached)
		{
			handler.SetState(new LiveTradingState());
		}
	}
}