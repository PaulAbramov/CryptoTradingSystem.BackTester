using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Database.Models;
using Serilog;
using System;

namespace CryptoTradingSystem.BackTester.StrategyHandler;

internal class ValidationState : IStrategyState
{
	private readonly DateTime startOfApprovementDuration;
	private readonly IChangeState stateChanger;

	public ValidationState(IChangeState stateChanger)
	{
		this.stateChanger = stateChanger;
		startOfApprovementDuration = DateTime.Now;
	}
	
	// open "papertrade" - "forwardtrade"
	public void OpenTrade(Asset entryCandle)
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
	public void CloseTrade(StrategyHandler handler, Asset closeCandle)
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
		
		//handler.Statistics
		
		var statisticsReached = true;
		if (statisticsReached)
		{
			stateChanger.ChangeState(new LiveTradingState(stateChanger));
		}
	}
}