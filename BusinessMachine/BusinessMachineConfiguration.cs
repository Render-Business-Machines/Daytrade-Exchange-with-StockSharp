namespace StockSharp.BusinessMachine;

using System;
using System.Collections.Generic;

/// <summary>
/// Configuration for business machine.
/// </summary>
public class BusinessMachineConfiguration
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BusinessMachineConfiguration"/>.
	/// </summary>
	public BusinessMachineConfiguration()
	{
		Rules = new List<BusinessRuleConfiguration>();
		Workflows = new Dictionary<string, WorkflowConfiguration>();
	}

	/// <summary>
	/// Gets or sets the machine name.
	/// </summary>
	public string MachineName { get; set; }

	/// <summary>
	/// Gets or sets the maximum concurrent trades.
	/// </summary>
	public int MaxConcurrentTrades { get; set; } = 10;

	/// <summary>
	/// Gets or sets the maximum position size.
	/// </summary>
	public decimal MaxPositionSize { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the risk percentage per trade.
	/// </summary>
	public decimal RiskPercentagePerTrade { get; set; } = 2;

	/// <summary>
	/// Gets or sets the trading start time.
	/// </summary>
	public TimeSpan? TradingStartTime { get; set; }

	/// <summary>
	/// Gets or sets the trading end time.
	/// </summary>
	public TimeSpan? TradingEndTime { get; set; }

	/// <summary>
	/// Gets or sets whether to enable auto cleanup of completed workflows.
	/// </summary>
	public bool EnableAutoCleanup { get; set; } = true;

	/// <summary>
	/// Gets or sets the cleanup interval in seconds.
	/// </summary>
	public int CleanupIntervalSeconds { get; set; } = 60;

	/// <summary>
	/// Gets the business rules configuration.
	/// </summary>
	public List<BusinessRuleConfiguration> Rules { get; }

	/// <summary>
	/// Gets the workflows configuration.
	/// </summary>
	public Dictionary<string, WorkflowConfiguration> Workflows { get; }

	/// <summary>
	/// Gets or sets additional settings.
	/// </summary>
	public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();

	/// <summary>
	/// Validates the configuration.
	/// </summary>
	/// <returns>Validation errors, if any.</returns>
	public List<string> Validate()
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(MachineName))
			errors.Add("MachineName is required");

		if (MaxConcurrentTrades <= 0)
			errors.Add("MaxConcurrentTrades must be greater than zero");

		if (MaxPositionSize <= 0)
			errors.Add("MaxPositionSize must be greater than zero");

		if (RiskPercentagePerTrade <= 0 || RiskPercentagePerTrade > 100)
			errors.Add("RiskPercentagePerTrade must be between 0 and 100");

		if (TradingStartTime.HasValue && TradingEndTime.HasValue && TradingStartTime >= TradingEndTime)
			errors.Add("TradingStartTime must be before TradingEndTime");

		if (CleanupIntervalSeconds <= 0)
			errors.Add("CleanupIntervalSeconds must be greater than zero");

		return errors;
	}

	/// <summary>
	/// Creates a default configuration.
	/// </summary>
	/// <returns>The default configuration.</returns>
	public static BusinessMachineConfiguration CreateDefault()
	{
		return new BusinessMachineConfiguration
		{
			MachineName = "DefaultDaytradeExchange",
			MaxConcurrentTrades = 10,
			MaxPositionSize = 1000,
			RiskPercentagePerTrade = 2,
			TradingStartTime = new TimeSpan(9, 30, 0), // 9:30 AM
			TradingEndTime = new TimeSpan(16, 0, 0),    // 4:00 PM
			EnableAutoCleanup = true,
			CleanupIntervalSeconds = 60
		};
	}
}

/// <summary>
/// Configuration for a business rule.
/// </summary>
public class BusinessRuleConfiguration
{
	/// <summary>
	/// Gets or sets the rule name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets whether the rule is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the rule type.
	/// </summary>
	public string RuleType { get; set; }

	/// <summary>
	/// Gets or sets the rule parameters.
	/// </summary>
	public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Configuration for a workflow.
/// </summary>
public class WorkflowConfiguration
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WorkflowConfiguration"/>.
	/// </summary>
	public WorkflowConfiguration()
	{
		Steps = new List<WorkflowStepConfiguration>();
	}

	/// <summary>
	/// Gets or sets the workflow name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets whether the workflow is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets the workflow steps.
	/// </summary>
	public List<WorkflowStepConfiguration> Steps { get; }

	/// <summary>
	/// Gets or sets the timeout in seconds.
	/// </summary>
	public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Configuration for a workflow step.
/// </summary>
public class WorkflowStepConfiguration
{
	/// <summary>
	/// Gets or sets the step name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the step type.
	/// </summary>
	public string StepType { get; set; }

	/// <summary>
	/// Gets or sets whether the step is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the step parameters.
	/// </summary>
	public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}
