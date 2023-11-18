using CryptoTradingSystem.General.Database.Models;

namespace CryptoTradingSystem.BackTester.Interfaces;

public interface IStrategyState
{
	void OpenTrade(Asset entryCandle);
	void CloseTrade(StrategyHandler.StrategyHandler handler, Asset closeCandle);
}