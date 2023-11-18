using CryptoTradingSystem.BackTester.StrategyHandler;
using CryptoTradingSystem.General.Database.Models;

namespace CryptoTradingSystem.Backtester.Tests;

[TestFixture]
public class StrategyHandlerTestsOpenTrades
{
	private StrategyHandler strategy = null!;
	
	[SetUp]
	public void Setup() => strategy = new StrategyHandler("TestStrategy", 1000);

	[Test]
	[TestCaseSource(nameof(GetTestCases_TestOpenTrade_OpenCandle_Null))]
	public void TestOpenTrade_OpenCandle_Null(Enums.TradeType tradeType, Asset openCandle)
	{
		// xArrange
		
		// Act
		// Assert
		var ex = Assert.Throws<ArgumentException>(
			() => strategy.OpenTrade(tradeType, openCandle));

		Assert.That(ex!.Message, Is.EqualTo("Parameter openCandle is null (Parameter 'openCandle')"));
	}
	
	[Test]
	[TestCaseSource(nameof(GetTestCases_TestOpenTrade_TradeType_None))]
	public void TestCloseTrade_TradeType_Null(Enums.TradeType tradeType, Asset openCandle)
	{
		// xArrange
		
		// Act
		// Assert
		var ex = Assert.Throws<ArgumentException>(
			() => strategy.OpenTrade(tradeType, openCandle));

		Assert.That(ex!.Message, Is.EqualTo("Parameter tradeType is None (Parameter 'tradeType')"));
	}
	
	[Test]
	[TestCaseSource(nameof(GetTestCases_TestOpenTrade))]
	public void TestOpenTrade_Already_Existing_Trade(Enums.TradeType tradeType, Asset openCandle)
	{
		// xArrange
		
		// Act
		strategy.OpenTrade(tradeType, openCandle);
		
		// Assert
		var ex = Assert.Throws<ArgumentException>(
			() => strategy.OpenTrade(tradeType, openCandle));
		
		Assert.That(ex!.Message, Is.EqualTo("TradeType already exists (Parameter 'tradeType')"));
	}

	[Test]
	[TestCaseSource(nameof(GetTestCases_TestOpenTrade))]
	public void TestOpenTrade_BackTestingState(Enums.TradeType tradeType, Asset openCandle)
	{
		// xArrange
		
		// Act
		strategy.OpenTrade(tradeType, openCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(1),
					"OpenTrades.Count");
				Assert.That(
					strategy.OpenTrades[Enums.TradeType.Buy],
					Is.EqualTo(100),
					"OpenTrades[Enums.TradeType.Buy]");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as BacktestingState),
					"CurrentState");
			});
	}
	
	[Test]
	[TestCaseSource(nameof(GetTestCases_TestOpenTrade))]
	public void TestOpenTrade_ValidationState(Enums.TradeType tradeType, Asset openCandle)
	{
		// xArrange
		strategy.SetState(new ValidationState());
		
		// Act
		strategy.OpenTrade(tradeType, openCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(1),
					"OpenTrades.Count");
				Assert.That(
					strategy.OpenTrades[Enums.TradeType.Buy],
					Is.EqualTo(100),
					"OpenTrades[Enums.TradeType.Buy]");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as ValidationState),
					"CurrentState");
			});
	}
	
	[Test]
	[TestCaseSource(nameof(GetTestCases_TestOpenTrade))]
	public void TestOpenTrade_LiveTradingState(Enums.TradeType tradeType, Asset openCandle)
	{
		// xArrange
		strategy.SetState(new LiveTradingState());
		
		// Act
		strategy.OpenTrade(tradeType, openCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(1),
					"OpenTrades.Count");
				Assert.That(
					strategy.OpenTrades[Enums.TradeType.Buy],
					Is.EqualTo(100),
					"OpenTrades[Enums.TradeType.Buy]");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as LiveTradingState),
					"CurrentState");
			});
	}
	
	/// <summary>
	/// Open trade with null as open candle
	/// </summary>
	/// <returns>long trades</returns>
	private static IEnumerable<TestCaseData> GetTestCases_TestOpenTrade_OpenCandle_Null()
	{
		yield return new TestCaseData(
			Enums.TradeType.Buy,
			null);
	}
	
	/// <summary>
	/// Open with "None" as tradetype
	/// </summary>
	/// <returns>long trades</returns>
	private static IEnumerable<TestCaseData> GetTestCases_TestOpenTrade_TradeType_None()
	{
		yield return new TestCaseData(
			Enums.TradeType.None,
			new Asset()
			{
				CandleClose = 120,
			});
	}
	
	/// <summary>
	///   Open simple trade
	/// </summary>
	/// <returns>Long trades</returns>
	private static IEnumerable<TestCaseData> GetTestCases_TestOpenTrade()
	{
		yield return new TestCaseData(
			Enums.TradeType.Buy,
			new Asset() { CandleClose = 100 });
	}
}

[TestFixture]
public class StrategyHandlerTestsCloseTrades
{
	private StrategyHandler strategy = null!;

	[SetUp]
	public void Setup()
	{
		strategy = new StrategyHandler("TestStrategy", 100);

		strategy.OpenTrade(Enums.TradeType.Buy, new Asset() { CandleClose = 100 });
	}

	[Test]
	[Category("Exception")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade_CloseCandle_Null))]
	public void TestCloseTrade_CloseCandle_Null(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		
		// Act
		// Assert
		var ex = Assert.Throws<ArgumentException>(
			() => strategy.CloseTrade(tradeTypeToClose, closeCandle));

		Assert.That(ex!.Message, Is.EqualTo("Parameter closeCandle is null (Parameter 'closeCandle')"));
	}
	
	[Test]
	[Category("Exception")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade_TradeType_None))]
	public void TestCloseTrade_TradeType_None(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		
		// Act
		// Assert
		var ex = Assert.Throws<ArgumentException>(
			() => strategy.CloseTrade(tradeTypeToClose, closeCandle));

		Assert.That(ex!.Message, Is.EqualTo("Parameter tradeTypeToClose is None (Parameter 'tradeTypeToClose')"));
	}
	
	[Test]
	[Category("SimpleCloseTrade")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade))]
	public void TestCloseTrade_BackTestingState(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		closeCandle.CloseTime = DateTime.Today.AddHours(22);

		// Act
		strategy.CloseTrade(tradeTypeToClose, closeCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(0),
					"OpenTrades.Count");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as BacktestingState),
					"CurrentState");
				Assert.That(
					strategy.Statistics.TradesAmount,
					Is.EqualTo(1),
					"strategy.Statistics.TradesAmount");
				Assert.That(
					strategy.Statistics.ProfitLoss,
					Is.EqualTo(-20),
					"strategy.Statistics.ProfitLoss");
				Assert.That(
					strategy.Statistics.ReturnOnInvestment,
					Is.EqualTo(-20),
					"strategy.Statistics.ReturnOnInvestment");
				Assert.That(
					strategy.Statistics.WonTradesPercentage,
					Is.EqualTo(0),
					"strategy.Statistics.WonTradesPercentage");
				Assert.That(
					strategy.Statistics.LostTradesPercentage,
					Is.EqualTo(100),
					"strategy.Statistics.LostTradesPercentage");
			});
	}
	
	[Test]
	[Category("SimpleCloseTrade")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade))]
	public void TestCloseTrade_BackTestingState_Into_ValidationState(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		closeCandle.CloseTime = DateTime.Today.AddHours(24);
		
		// Act
		strategy.CloseTrade(tradeTypeToClose, closeCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(0),
					"OpenTrades.Count");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as ValidationState),
					"CurrentState");
				Assert.That(
					strategy.Statistics.TradesAmount,
					Is.EqualTo(1),
					"strategy.Statistics.TradesAmount");
				Assert.That(
					strategy.Statistics.ProfitLoss,
					Is.EqualTo(-20),
					"strategy.Statistics.ProfitLoss");
				Assert.That(
					strategy.Statistics.ReturnOnInvestment,
					Is.EqualTo(-20),
					"strategy.Statistics.ReturnOnInvestment");
				Assert.That(
					strategy.Statistics.WonTradesPercentage,
					Is.EqualTo(0),
					"strategy.Statistics.WonTradesPercentage");
				Assert.That(
					strategy.Statistics.LostTradesPercentage,
					Is.EqualTo(100),
					"strategy.Statistics.LostTradesPercentage");
			});
	}
	
	[Test]
	[Category("SimpleCloseTrade")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade))]
	public void TestCloseTrade_ValidationState(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		strategy.SetState(new ValidationState());
		strategy.ApprovementDuration = TimeSpan.FromHours(1);
		
		// Act
		strategy.CloseTrade(tradeTypeToClose, closeCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(0),
					"OpenTrades.Count");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as ValidationState),
					"CurrentState");
				Assert.That(
					strategy.Statistics.TradesAmount,
					Is.EqualTo(1),
					"strategy.Statistics.TradesAmount");
				Assert.That(
					strategy.Statistics.ProfitLoss,
					Is.EqualTo(-20),
					"strategy.Statistics.ProfitLoss");
				Assert.That(
					strategy.Statistics.ReturnOnInvestment,
					Is.EqualTo(-20),
					"strategy.Statistics.ReturnOnInvestment");
				Assert.That(
					strategy.Statistics.WonTradesPercentage,
					Is.EqualTo(0),
					"strategy.Statistics.WonTradesPercentage");
				Assert.That(
					strategy.Statistics.LostTradesPercentage,
					Is.EqualTo(100),
					"strategy.Statistics.LostTradesPercentage");
			});
	}
	
	[Test]
	[Category("SimpleCloseTrade")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade))]
	public void TestCloseTrade_ValidationState_Into_LiveTradingState(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		strategy.SetState(new ValidationState());
		// No ApprovementDuration set, so it will calculate the statistics
		
		// Act
		strategy.CloseTrade(tradeTypeToClose, closeCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(0),
					"OpenTrades.Count");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as LiveTradingState),
					"CurrentState");
				Assert.That(
					strategy.Statistics.TradesAmount,
					Is.EqualTo(1),
					"strategy.Statistics.TradesAmount");
				Assert.That(
					strategy.Statistics.ProfitLoss,
					Is.EqualTo(-20),
					"strategy.Statistics.ProfitLoss");
				Assert.That(
					strategy.Statistics.ReturnOnInvestment,
					Is.EqualTo(-20),
					"strategy.Statistics.ReturnOnInvestment");
				Assert.That(
					strategy.Statistics.WonTradesPercentage,
					Is.EqualTo(0),
					"strategy.Statistics.WonTradesPercentage");
				Assert.That(
					strategy.Statistics.LostTradesPercentage,
					Is.EqualTo(100),
					"strategy.Statistics.LostTradesPercentage");
			});
	}
	
	[Test]
	[Category("SimpleCloseTrade")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade))]
	public void TestCloseTrade_LiveTradingState(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		strategy.SetState(new LiveTradingState());
		closeCandle.CloseTime = DateTime.Today.AddHours(22);
		
		// Act
		strategy.CloseTrade(tradeTypeToClose, closeCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(0),
					"OpenTrades.Count");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as LiveTradingState),
					"CurrentState");
				Assert.That(
					strategy.Statistics.TradesAmount,
					Is.EqualTo(1),
					"strategy.Statistics.TradesAmount");
				Assert.That(
					strategy.Statistics.ProfitLoss,
					Is.EqualTo(-20),
					"strategy.Statistics.ProfitLoss");
				Assert.That(
					strategy.Statistics.ReturnOnInvestment,
					Is.EqualTo(-20),
					"strategy.Statistics.ReturnOnInvestment");
				Assert.That(
					strategy.Statistics.WonTradesPercentage,
					Is.EqualTo(0),
					"strategy.Statistics.WonTradesPercentage");
				Assert.That(
					strategy.Statistics.LostTradesPercentage,
					Is.EqualTo(100),
					"strategy.Statistics.LostTradesPercentage");
			});
	}
	
	[Test]
	[Category("SimpleCloseTrade")]
	[TestCaseSource(nameof(GetTestCases_TestCloseTrade))]
	public void TestCloseTrade_LiveTradingState_Into_ValidationState(Enums.TradeType tradeTypeToClose, Asset closeCandle)
	{
		// xArrange
		strategy.SetState(new LiveTradingState());
		closeCandle.CloseTime = DateTime.Today.AddHours(24);
		
		// Act
		strategy.CloseTrade(tradeTypeToClose, closeCandle);
		
		// Assert
		Assert.Multiple(
			() =>
			{
				// Assert
				// check if stats are correct
				Assert.That(
					strategy.OpenTrades,
					Has.Count.EqualTo(0),
					"OpenTrades.Count");
				Assert.That(
					strategy.GetState(),
					Is.EqualTo(strategy.GetState() as ValidationState),
					"CurrentState");
				Assert.That(
					strategy.Statistics.TradesAmount,
					Is.EqualTo(1),
					"strategy.Statistics.TradesAmount");
				Assert.That(
					strategy.Statistics.ProfitLoss,
					Is.EqualTo(-20),
					"strategy.Statistics.ProfitLoss");
				Assert.That(
					strategy.Statistics.ReturnOnInvestment,
					Is.EqualTo(-20),
					"strategy.Statistics.ReturnOnInvestment");
				Assert.That(
					strategy.Statistics.WonTradesPercentage,
					Is.EqualTo(0),
					"strategy.Statistics.WonTradesPercentage");
				Assert.That(
					strategy.Statistics.LostTradesPercentage,
					Is.EqualTo(100),
					"strategy.Statistics.LostTradesPercentage");
			});
	}

	/// <summary>
	///   Close simple trade with null as close candle
	/// </summary>
	/// <returns>Close long trades</returns>
	private static IEnumerable<TestCaseData> GetTestCases_TestCloseTrade_CloseCandle_Null()
	{
		yield return new TestCaseData(
			Enums.TradeType.Buy,
			null);
	}
	
	/// <summary>
	///   Close simple trade with "None" as tradetype
	/// </summary>
	/// <returns>Close long trades</returns>
	private static IEnumerable<TestCaseData> GetTestCases_TestCloseTrade_TradeType_None()
	{
		yield return new TestCaseData(
			Enums.TradeType.None,
			new Asset()
			{
				CandleClose = 120,
			});
	}
	
	/// <summary>
	///	Close simple trade
	///	Some Data has to be given for statistics calculation	
	/// </summary>
	/// <returns>Close long trades</returns>
	private static IEnumerable<TestCaseData> GetTestCases_TestCloseTrade()
	{
		yield return new TestCaseData(
			Enums.TradeType.Buy,
			new Asset()
			{
				CandleClose = 120
			});
	}
}