using System;

namespace CryptoTradingSystem.BackTester;

public class RunningStrategy
{
    public string Name { get; init; }
    public DateTime CurrentCloseDateTime { get; set; }
    public bool RunningTrade { get; set; }
    public int TradesAmount { get; set; }
}