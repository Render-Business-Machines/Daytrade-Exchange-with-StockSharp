namespace StockSharp.BusinessMachine;

/// <summary>
/// Represents the possible states of a business machine.
/// </summary>
public enum BusinessMachineState
{
	/// <summary>
	/// Initial state when the machine is created.
	/// </summary>
	Initialized,

	/// <summary>
	/// Machine is starting up and initializing resources.
	/// </summary>
	Starting,

	/// <summary>
	/// Machine is running and processing business logic.
	/// </summary>
	Running,

	/// <summary>
	/// Machine is paused and not processing new events.
	/// </summary>
	Paused,

	/// <summary>
	/// Machine is stopping and cleaning up resources.
	/// </summary>
	Stopping,

	/// <summary>
	/// Machine has stopped.
	/// </summary>
	Stopped,

	/// <summary>
	/// Machine has encountered an error.
	/// </summary>
	Error
}

/// <summary>
/// Represents the type of business machine operation.
/// </summary>
public enum BusinessMachineOperation
{
	/// <summary>
	/// Trade execution operation.
	/// </summary>
	TradeExecution,

	/// <summary>
	/// Order management operation.
	/// </summary>
	OrderManagement,

	/// <summary>
	/// Risk assessment operation.
	/// </summary>
	RiskAssessment,

	/// <summary>
	/// Portfolio management operation.
	/// </summary>
	PortfolioManagement,

	/// <summary>
	/// Market data processing operation.
	/// </summary>
	MarketDataProcessing,

	/// <summary>
	/// Strategy execution operation.
	/// </summary>
	StrategyExecution
}
