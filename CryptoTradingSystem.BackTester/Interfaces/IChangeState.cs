namespace CryptoTradingSystem.BackTester.Interfaces;

// With this interface only specific classes are allowed to change the state
internal interface IChangeState
{
	void ChangeState(IStrategyState state);
}