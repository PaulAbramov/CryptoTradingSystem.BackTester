namespace CryptoTradingSystem.BackTester;

public class StrategyOption
{
    public string Name { get; set; }
    public string Path { get; set; }
    public EStrategyActivityState ActivityState { get; set; }
}

public enum EStrategyActivityState
{
    None = 0,
    Enabled = 1,
    ToDelete = 2
}