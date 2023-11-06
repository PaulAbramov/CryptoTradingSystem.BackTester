using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;
using System;

namespace CryptoTradingSystem.BackTester.Interfaces;

public interface IDatabaseHandlerBackTester
{
	T? GetIndicator<T>(
		Enums.Assets asset,
		Enums.TimeFrames timeFrame,
		Type indicator,
		DateTime firstCloseTime = new(),
		DateTime lastCloseTime = new())
		where T : Indicator;
}