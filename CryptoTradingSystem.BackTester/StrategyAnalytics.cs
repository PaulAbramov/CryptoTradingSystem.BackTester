namespace CryptoTradingSystem.BackTester
{
    public class StrategyAnalytics
    {
        public decimal ProfitLoss { get; set; }
        public decimal ReturnOnInvestment { get; set; }
        public decimal TradesAmount { get; set; }
        public decimal AmountOfWonTrades { get; set; }
        public decimal AmountOfLostTrades { get; set; }
        public decimal WonTradesPercentage { get; set; }
        public decimal LostTradesPercentage { get; set; }
        public decimal RiskRewardRatio { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal SortinoRatio { get; set; }
    }
}
