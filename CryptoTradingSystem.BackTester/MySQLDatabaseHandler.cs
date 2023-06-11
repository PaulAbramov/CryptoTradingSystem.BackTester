using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTradingSystem.BackTester.Interfaces;
using CryptoTradingSystem.General.Data;
using CryptoTradingSystem.General.Database;
using CryptoTradingSystem.General.Database.Models;
using CryptoTradingSystem.General.Strategy;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CryptoTradingSystem.BackTester;

public class MySQLDatabaseHandler : IDatabaseHandlerBackTester
{
    private readonly string connectionString;

    public MySQLDatabaseHandler(string connectionString)
    {
        this.connectionString = connectionString;
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
            using var contextDb = new CryptoTradingSystemContext(connectionString);

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
    
    /// <summary>
    /// iterates over all assets and gets the data from the database
    /// </summary>
    /// <param name="strategyParameter"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static List<Indicator> GetDataFromDatabase(StrategyParameter strategyParameter, string connectionString)
    {
        var results = new List<Indicator>();
        foreach(var asset in strategyParameter.Assets)
        {
            try
            {
                var databaseHandler = new MySQLDatabaseHandler(connectionString);
    
                //make the call to "GetIndicators" Generic
                var databaseHandlerType = databaseHandler.GetType();
    
                var method = databaseHandlerType.GetMethod("GetIndicator");
                var genericMethod = method?.MakeGenericMethod(asset.Item3);
                
                //pass the parameters to the method
                var result = (Indicator?) genericMethod?.Invoke(
                    databaseHandler, 
                    new object?[]
                    {
                        asset.Item2,
                        asset.Item1,
                        asset.Item3,
                        strategyParameter.TimeFrameStart,
                        strategyParameter.TimeFrameEnd
                    });
    
                if (result is not null)
                {
                    results.Add(result);
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "{Asset} | {TimeFrame} | {Indicator} | {LastClose} | could not get indicators from Database",
                    Enums.Assets.Btcusdt.GetStringValue(),
                    Enums.TimeFrames.M5.GetStringValue(),
                    asset.Item3.Name,
                    DateTime.Now.AddMonths(-1));
    
                throw;
            }
        }
    
        return results;
    }
}