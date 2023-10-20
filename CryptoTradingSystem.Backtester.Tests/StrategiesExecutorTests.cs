using CryptoTradingSystem.BackTester;
using CryptoTradingSystem.General.Data;

namespace CryptoTradingSystem.Backtester.Tests
{
    [TestFixture]
    public class StrategiesExecutorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases_TestCalculateStatistics_Won))]
        [TestCaseSource(nameof(GetTestCases_TestCalculateStatistics_Lost))]
        [TestCaseSource(nameof(GetTestCases_TestCalculateStatistics_Failures))]
        public void TestCalculateStatistics(
            RunningStrategy strategy,
            decimal candleClose,
            Enums.TradeType tradeType,
            bool expectedRunningStrategy,
            StrategyAnalytics expectedStrategyAnalytics)
        {
            // xArrange

            // Act
            StrategiesExecutor.SetStatisticsClosedTrades(strategy,
                                               candleClose,
                                               tradeType);
            Assert.Multiple(() =>
            {
                // Assert
                // check if stats are correct
                Assert.That(strategy.StrategyAnalytics.ProfitLoss, Is.EqualTo(expectedStrategyAnalytics.ProfitLoss), "ProfitLoss");
                Assert.That(strategy.StrategyAnalytics.ReturnOnInvestment, Is.EqualTo(expectedStrategyAnalytics.ReturnOnInvestment), "ReturnOnInvestment");
                Assert.That(strategy.StrategyAnalytics.TradesAmount, Is.EqualTo(expectedStrategyAnalytics.TradesAmount), "TradesAmount");
                Assert.That(strategy.StrategyAnalytics.AmountOfWonTrades, Is.EqualTo(expectedStrategyAnalytics.AmountOfWonTrades), "AmountOfWonTrades");
                Assert.That(strategy.StrategyAnalytics.WonTradesPercentage, Is.EqualTo(expectedStrategyAnalytics.WonTradesPercentage), "WonTradesPercentage");
                Assert.That(strategy.StrategyAnalytics.AmountOfLostTrades, Is.EqualTo(expectedStrategyAnalytics.AmountOfLostTrades), "AmountOfLostTrades");
                Assert.That(strategy.StrategyAnalytics.LostTradesPercentage, Is.EqualTo(expectedStrategyAnalytics.LostTradesPercentage), "LostTradesPercentage");
                Assert.That(strategy.StrategyAnalytics.SharpeRatio, Is.EqualTo(expectedStrategyAnalytics.SharpeRatio), "SharpeRatio");
                Assert.That(strategy.StrategyAnalytics.SortinoRatio, Is.EqualTo(expectedStrategyAnalytics.SortinoRatio), "SortinoRatio");
                Assert.That(strategy.RunningTrade, Is.EqualTo(expectedRunningStrategy), "RunningTrade");
            });
        }

        /// <summary>
        /// 1. Case first simple trade
        /// 2. Case first more complex trade
        /// </summary>
        /// <returns>Long trades closed with sell in profit</returns>
        private static IEnumerable<TestCaseData> GetTestCases_TestCalculateStatistics_Won()
        {
            yield return new TestCaseData(
                new RunningStrategy { RunningTrade = true, TradeOpenPrice = 100m, InitialInvestment = 100m },
                110m,
                Enums.TradeType.Sell,
                false,
                new StrategyAnalytics()
                {
                    ProfitLoss = 10m,
                    ReturnOnInvestment = 10m,
                    TradesAmount = 1m,
                    AmountOfWonTrades = 1m,
                    WonTradesPercentage = 100m,
                    AmountOfLostTrades = 0m,
                    LostTradesPercentage = 0m,
                    SharpeRatio = 0m,
                    SortinoRatio = 0m,
                });

            yield return new TestCaseData(
                new RunningStrategy
                {
                    RunningTrade = true,
                    TradeOpenPrice = 100m,
                    InitialInvestment = 100m,
                    StrategyAnalytics = new StrategyAnalytics()
                    {
                        TradesAmount = 9m,
                        AmountOfWonTrades = 4m,
                        WonTradesPercentage = 40m,
                        AmountOfLostTrades = 5m,
                        LostTradesPercentage = 50m,
                        SharpeRatio = 0m,
                        SortinoRatio = 0m
                    }
                },
                110m,
                Enums.TradeType.Sell,
                false,
                new StrategyAnalytics()
                {
                    ProfitLoss = 10m,
                    ReturnOnInvestment = 10m,
                    TradesAmount = 10m,
                    AmountOfWonTrades = 5m,
                    WonTradesPercentage = 50m,
                    AmountOfLostTrades = 5m,
                    LostTradesPercentage = 50m,
                    SharpeRatio = 0m,
                    SortinoRatio = 0m,
                });
        }

        /// <summary>
        /// 1. Case first simple trade
        /// 2. Case first more complex trade
        /// </summary>
        /// <returns>Long trades closed with sell in loss</returns>
        private static IEnumerable<TestCaseData> GetTestCases_TestCalculateStatistics_Lost()
        {
            yield return new TestCaseData(
                new RunningStrategy { RunningTrade = true, TradeOpenPrice = 100m, InitialInvestment = 100m },
                110m,
                Enums.TradeType.Buy,
                false,
                new StrategyAnalytics()
                {
                    ProfitLoss = -10m,
                    ReturnOnInvestment = -10m,
                    TradesAmount = 1m,
                    AmountOfWonTrades = 0m,
                    WonTradesPercentage = 0m,
                    AmountOfLostTrades = 1m,
                    LostTradesPercentage = 100m,
                    SharpeRatio = 0m,
                    SortinoRatio = 0m,
                });

            yield return new TestCaseData(
                new RunningStrategy
                {
                    RunningTrade = true,
                    TradeOpenPrice = 100m,
                    InitialInvestment = 100m,
                    StrategyAnalytics = new StrategyAnalytics()
                    {
                        TradesAmount = 9m,
                        AmountOfWonTrades = 5m,
                        WonTradesPercentage = 50m,
                        AmountOfLostTrades = 4m,
                        LostTradesPercentage = 40m,
                        SharpeRatio = 0m,
                        SortinoRatio = 0m
                    }
                },
                110m,
                Enums.TradeType.Buy,
                false,
                new StrategyAnalytics()
                {
                    ProfitLoss = -10m,
                    ReturnOnInvestment = -10m,
                    TradesAmount = 10m,
                    AmountOfWonTrades = 5m,
                    WonTradesPercentage = 50m,
                    AmountOfLostTrades = 5m,
                    LostTradesPercentage = 50m,
                    SharpeRatio = 0m,
                    SortinoRatio = 0m,
                });
        }

        /// <summary>
        /// 1. Case TradeType is None       - "Runningtrade" results in true    - Statistics are not calculated
        /// 2. case RunningTrade is false   - Method returns in the beginning   - Statistics are not calculated
        /// </summary>
        /// <returns>Long trades closed with sell with failed Data</returns>
        private static IEnumerable<TestCaseData> GetTestCases_TestCalculateStatistics_Failures()
        {
            yield return new TestCaseData(
                new RunningStrategy { RunningTrade = true, TradeOpenPrice = 100m },
                110m,
                Enums.TradeType.None,
                true,
                new StrategyAnalytics()
                {
                    ProfitLoss = 0m,
                    TradesAmount = 0m,
                    AmountOfWonTrades = 0m,
                    WonTradesPercentage = 0m,
                    AmountOfLostTrades = 0m,
                    LostTradesPercentage = 0m,
                    SharpeRatio = 0m,
                    SortinoRatio = 0m,
                });

            yield return new TestCaseData(
                new RunningStrategy { RunningTrade = false, TradeOpenPrice = 100m },
                110m,
                Enums.TradeType.None,
                false,
                new StrategyAnalytics()
                {
                    ProfitLoss = 0m,
                    TradesAmount = 0m,
                    AmountOfWonTrades = 0m,
                    WonTradesPercentage = 0m,
                    AmountOfLostTrades = 0m,
                    LostTradesPercentage = 0m,
                    SharpeRatio = 0m,
                    SortinoRatio = 0m,
                });
        }
    }
}