using CryptoTradingSystem.General.Database.Models;
using System.Net.Http;

namespace CryptoTradingSystem.BackTester.Interfaces;

public abstract class StrategyState
{
	public abstract void OpenTrade(StrategyHandler.StrategyHandler handler, Asset entryCandle);
	public abstract void CloseTrade(StrategyHandler.StrategyHandler handler, Asset closeCandle);
}