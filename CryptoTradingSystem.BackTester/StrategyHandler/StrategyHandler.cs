using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoTradingSystem.BackTester.StrategyHandler;

public class StrategyHandler
{
	public string Name { get; init; }
	//TODO CurrentCloseDateTime needed? I have candle closeTime
	public DateTime CurrentCloseDateTime { get; set; }
	public bool RunningTrade { get; set; }
	public int TradesAmount { get; set; }
	public TimeSpan ApprovementDuration { get; set; }
	public StrategyStatistics Statistics { get; private set; } = new();
	public StrategyStatistics ApprovementStatistics { get; private set; } = new();
	public Dictionary<Enums.TradeType, decimal> OpenTrades { get; }= new();
	
	private IStrategyState CurrentState { get; set; } = new BacktestingState();

	private Asset? entryCandle;

	public StrategyHandler(string name, decimal initialInvestment)
	{
		Name = name;
		CurrentCloseDateTime = DateTime.MinValue;
		RunningTrade = false;
		TradesAmount = 0;
		Statistics.InitialInvestment = initialInvestment;
	}
	
	internal IStrategyState GetState() => CurrentState;

	public void OpenTrade(Enums.TradeType tradeType, Asset openCandle)
	{
		if (tradeType == Enums.TradeType.None)
		{
			throw new ArgumentException("Parameter tradeType is None", nameof(tradeType));
		}
		
		if (openCandle == null)
		{
			throw new ArgumentException("Parameter openCandle is null", nameof(openCandle));
		}
		
		if (!OpenTrades.TryAdd(tradeType, openCandle.CandleClose))
		{
			throw new ArgumentException("TradeType already exists", nameof(tradeType));
		}
			
		entryCandle = openCandle;
		CurrentState.OpenTrade(this, openCandle); 
	}

	public void CloseTrade(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		if (tradeTypeToClose == Enums.TradeType.None)
		{
			throw new ArgumentException("Parameter tradeTypeToClose is None", nameof(tradeTypeToClose));
		}
		
		if (closeCandle == null)
		{
			throw new ArgumentException("Parameter closeCandle is null", nameof(closeCandle));
		}
		
		if (entryCandle == null)
		{
			return;
		}

		entryCandle = null;
		
		CalculateStatistics(tradeTypeToClose, closeCandle);
		CurrentState.CloseTrade(this, closeCandle);
	}

	internal void SetState(IStrategyState state) => CurrentState = state;
	
	/// <summary>
	///   Set statistics after closing the trade
	/// </summary>
	private void CalculateStatistics(Enums.TradeType tradeType, Asset closeCandle)
	{
		var profitLoss = CalculateProfitLoss(closeCandle.CandleClose, tradeType);

		Statistics.TradesAmount++;

		if (profitLoss != null)
		{
			Statistics.ProfitLoss += profitLoss.Value;
			_ = profitLoss > 0 ? Statistics.AmountOfWonTrades++ : Statistics.AmountOfLostTrades++;
		}

		Statistics.ReturnOnInvestment = Statistics.ProfitLoss / Statistics.InitialInvestment * 100;

		Statistics.WonTradesPercentage = Statistics.AmountOfWonTrades / Statistics.TradesAmount * 100;
		Statistics.LostTradesPercentage = 100m - Statistics.WonTradesPercentage;

		// TODO calculate RiskReward Ratio
		// !ratio between potential profit and potential loss

		// TODO calculate Sharpe Ratio
		// !risk-adjusted return - considers return and volatility of strategy
		// !(Return of Portfolio - Risk-Free Rate) / Portfolio Standard Deviation
		// !(ROI - 2%) / BTC standard deviation
		//  strategy.StrategyAnalytics.SharpeRatio = (strategy.StrategyAnalytics.ReturnOnInvestment - 2m) / ;

		// TODO calculate Max Drawdown in %

		// TODO calculate Average Trade Duration
		// !shows holding time and strategy efficiency

		// TODO calculate Trade Frequency 30D?

		// TODO calculate Slippage
		// !analyze difference between expected price and execute price of orders

		// TODO calculate Volatility
	}
	
	private decimal? CalculateProfitLoss(decimal candleClose, Enums.TradeType tradeType)
	{
		decimal? profitLoss = null;
		switch (tradeType)
		{
			case Enums.TradeType.Buy:
				var buyTrade = OpenTrades.FirstOrDefault(x => x.Key == Enums.TradeType.Buy);
				profitLoss = buyTrade.Value - candleClose;
				OpenTrades.Remove(buyTrade.Key);
				break;
			case Enums.TradeType.Sell:
				var sellTrade = OpenTrades.FirstOrDefault(x => x.Key == Enums.TradeType.Sell);
				profitLoss = candleClose - sellTrade.Value;
				OpenTrades.Remove(sellTrade.Key);
				break;
			case Enums.TradeType.None:
			default:
				break;
		}

		return profitLoss;
	}
}