namespace CryptoTradingSystem.BackTester
{
    public class StrategyAnalytics
    {
        public decimal Profit { get; set; }
        public int TradesAmount { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal SortinoRatio { get; set; }
        public int AmountOfWonTrades { get; set; }
        public int AmountOfLostTrades { get; set; }
        public decimal WonTradesPercentage { get; set; }
        public decimal LostTradesPercentage { get; set; }
    }
}
