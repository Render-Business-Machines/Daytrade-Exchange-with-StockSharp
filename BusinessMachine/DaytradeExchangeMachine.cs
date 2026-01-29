namespace StockSharp.BusinessMachine;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo;

/// <summary>
/// Business machine implementation for daytrade exchange operations.
/// Manages the workflow and business logic for day trading activities.
/// </summary>
public class DaytradeExchangeMachine : BusinessMachine
{
	private readonly List<IBusinessRule> _rules = new List<IBusinessRule>();
	private readonly Dictionary<long, TradeWorkflow> _activeWorkflows = new Dictionary<long, TradeWorkflow>();
	private readonly object _workflowLock = new object();

	/// <summary>
	/// Initializes a new instance of the <see cref="DaytradeExchangeMachine"/>.
	/// </summary>
	public DaytradeExchangeMachine() : base("DaytradeExchange")
	{
		InitializeDefaultRules();
	}

	/// <summary>
	/// Gets or sets the connector for market operations.
	/// </summary>
	public IConnector Connector { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of concurrent trades.
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
	/// Adds a business rule to the machine.
	/// </summary>
	/// <param name="rule">The rule to add.</param>
	public void AddRule(IBusinessRule rule)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		_rules.Add(rule);
	}

	/// <summary>
	/// Removes a business rule from the machine.
	/// </summary>
	/// <param name="rule">The rule to remove.</param>
	public void RemoveRule(IBusinessRule rule)
	{
		_rules.Remove(rule);
	}

	/// <summary>
	/// Executes a trade through the business machine.
	/// </summary>
	/// <param name="security">The security to trade.</param>
	/// <param name="side">The side (buy/sell).</param>
	/// <param name="volume">The volume.</param>
	/// <returns>The workflow ID.</returns>
	public long ExecuteTrade(Security security, Sides side, decimal volume)
	{
		if (State != BusinessMachineState.Running)
			throw new InvalidOperationException($"Cannot execute trade while machine is in {State} state");

		// Create trade context
		var context = new TradeContext
		{
			Security = security,
			Side = side,
			Volume = volume,
			RequestTime = DateTimeOffset.Now
		};

		// Validate against all rules
		foreach (var rule in _rules)
		{
			var result = rule.Validate(context);
			if (!result.IsValid)
			{
				OnBusinessRuleViolation(rule.Name, result.Message);
				throw new BusinessRuleException(rule.Name, result.Message);
			}
		}

		// Create and start workflow
		var workflow = new TradeWorkflow(context);
		lock (_workflowLock)
		{
			_activeWorkflows[workflow.Id] = workflow;
		}

		workflow.Start();
		return workflow.Id;
	}

	/// <summary>
	/// Gets the status of a workflow.
	/// </summary>
	/// <param name="workflowId">The workflow ID.</param>
	/// <returns>The workflow status.</returns>
	public TradeWorkflowStatus GetWorkflowStatus(long workflowId)
	{
		lock (_workflowLock)
		{
			if (_activeWorkflows.TryGetValue(workflowId, out var workflow))
			{
				return workflow.Status;
			}
		}

		return TradeWorkflowStatus.NotFound;
	}

	/// <summary>
	/// Cancels a workflow.
	/// </summary>
	/// <param name="workflowId">The workflow ID.</param>
	public void CancelWorkflow(long workflowId)
	{
		lock (_workflowLock)
		{
			if (_activeWorkflows.TryGetValue(workflowId, out var workflow))
			{
				workflow.Cancel();
			}
		}
	}

	/// <summary>
	/// Gets all active workflows.
	/// </summary>
	/// <returns>The active workflows.</returns>
	public IEnumerable<TradeWorkflow> GetActiveWorkflows()
	{
		lock (_workflowLock)
		{
			return _activeWorkflows.Values.ToList();
		}
	}

	/// <summary>
	/// Cleans up completed workflows.
	/// </summary>
	public void CleanupCompletedWorkflows()
	{
		lock (_workflowLock)
		{
			var completed = _activeWorkflows
				.Where(kvp => kvp.Value.Status == TradeWorkflowStatus.Completed || 
				              kvp.Value.Status == TradeWorkflowStatus.Cancelled ||
				              kvp.Value.Status == TradeWorkflowStatus.Failed)
				.Select(kvp => kvp.Key)
				.ToList();

			foreach (var id in completed)
			{
				_activeWorkflows.Remove(id);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnStart()
	{
		if (Connector == null)
			throw new InvalidOperationException("Connector must be set before starting the machine");

		// Initialize machine components
		SetStateData("StartTime", DateTimeOffset.Now);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		// Cancel all active workflows
		lock (_workflowLock)
		{
			foreach (var workflow in _activeWorkflows.Values)
			{
				workflow.Cancel();
			}
			_activeWorkflows.Clear();
		}

		SetStateData("StopTime", DateTimeOffset.Now);
	}

	/// <inheritdoc />
	protected override void OnPause()
	{
		// Pause all active workflows
		lock (_workflowLock)
		{
			foreach (var workflow in _activeWorkflows.Values)
			{
				workflow.Pause();
			}
		}
	}

	/// <inheritdoc />
	protected override void OnResume()
	{
		// Resume all paused workflows
		lock (_workflowLock)
		{
			foreach (var workflow in _activeWorkflows.Values)
			{
				workflow.Resume();
			}
		}
	}

	private void InitializeDefaultRules()
	{
		// Add default business rules
		AddRule(new MaxConcurrentTradesRule(() => MaxConcurrentTrades, () => _activeWorkflows.Count));
		AddRule(new MaxPositionSizeRule(() => MaxPositionSize));
		AddRule(new RiskManagementRule(() => RiskPercentagePerTrade));
	}
}

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BusinessRuleException"/>.
	/// </summary>
	public BusinessRuleException(string ruleName, string message) 
		: base($"Business rule '{ruleName}' violated: {message}")
	{
		RuleName = ruleName;
	}

	/// <summary>
	/// Gets the rule name.
	/// </summary>
	public string RuleName { get; }
}
