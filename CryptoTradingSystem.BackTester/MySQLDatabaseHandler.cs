using System;
using System.Linq;
using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database;
using CryptoTradingSystem.General.Database.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CryptoTradingSystem.BackTester;

public class MySQLDatabaseHandler : IDatabaseHandlerBackTester
{
    private readonly string _connectionString;

    public MySQLDatabaseHandler(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    /// <summary>
    /// return indicators for timeframe
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="timeFrame"></param>
    /// <param name="indicator"></param>
    /// <param name="firstCloseTime"></param>
    /// <param name="lastCloseTime"></param>
    /// <returns></returns>
    public T? GetIndicator<T>(
        Enums.Assets asset, 
        Enums.TimeFrames timeFrame, 
        Type indicator,
        DateTime firstCloseTime = new DateTime(), 
        DateTime lastCloseTime = new DateTime()) 
        where T : Indicator
    {
        if (lastCloseTime == DateTime.MinValue)
        {
            lastCloseTime = DateTime.MaxValue;
        }
        
        try
        {
            using var contextDb = new CryptoTradingSystemContext(_connectionString);

            var property = typeof(CryptoTradingSystemContext).GetProperty($"{indicator.Name}s");

            if (property != null)
            {
                Log.Debug("{PropertyName} does match {Indicator}", property.Name, indicator.Name);

                var dbset = (DbSet<T>)property.GetValue(contextDb);
                if (dbset != null)
                {
                    var indicatorValue = dbset.Include(x => x.Asset)
                        .FirstOrDefault(x => x.AssetName == asset.GetStringValue()
                                             && x.Interval == timeFrame.GetStringValue()
                                             && x.CloseTime > firstCloseTime
                                             && x.CloseTime <= lastCloseTime);

                    return indicatorValue;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(
                e,
                "{Asset} | {TimeFrame} | {Indicator} | {FirstClose} | {LastClose} | could not get candles from Database", 
                asset.GetStringValue(), 
                timeFrame.GetStringValue(), 
                indicator.Name,
                firstCloseTime,
                lastCloseTime);
            throw;
        }

        return null;
    }
}