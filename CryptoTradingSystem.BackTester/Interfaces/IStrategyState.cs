using CryptoTradingSystem.General.Database.Models;

namespace CryptoTradingSystem.BackTester.Interfaces;

internal interface IStrategyState
{
	void OpenTrade(StrategyHandler.StrategyHandler handler, Asset entryCandle);
	void CloseTrade(StrategyHandler.StrategyHandler handler, Asset closeCandle);
}