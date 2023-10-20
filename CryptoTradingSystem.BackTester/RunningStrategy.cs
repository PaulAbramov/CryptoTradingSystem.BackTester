using System;

namespace CryptoTradingSystem.BackTester;

public class RunningStrategy
{
    public string Name { get; init; }
    public DateTime CurrentCloseDateTime { get; set; }
    public decimal TradeOpenPrice { get; set; }
    public bool RunningTrade { get; set; }
    public decimal InitialInvestment { get; set; }
    public StrategyAnalytics StrategyAnalytics { get; set; } = new();
}