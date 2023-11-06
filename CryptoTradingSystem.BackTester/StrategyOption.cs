namespace CryptoTradingSystem.BackTester;

public class StrategyOption
{
	public string Name { get; init; }
	public string Path { get; init; }
	public EStrategyActivityState ActivityState { get; set; }
}

public enum EStrategyActivityState
{
	None = 0,
	Enabled = 1,
	ToDelete = 2
}