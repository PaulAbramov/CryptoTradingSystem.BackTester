using System;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database.Models;

namespace CryptoTradingSystem.BackTester.Interfaces;

public interface IDatabaseHandlerBackTester
{
    T? GetIndicator<T>(
        Enums.Assets asset,
        Enums.TimeFrames timeFrame,
        Type indicator,
        DateTime firstCloseTime = new DateTime(),
        DateTime lastCloseTime = new DateTime())
        where T : Indicator;
}