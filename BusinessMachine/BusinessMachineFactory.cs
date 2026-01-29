namespace StockSharp.BusinessMachine;

using System;

using StockSharp.BusinessEntities;
using StockSharp.Algo;

/// <summary>
/// Factory for creating business machines.
/// </summary>
public class BusinessMachineFactory
{
	/// <summary>
	/// Creates a daytrade exchange machine with the specified configuration.
	/// </summary>
	/// <param name="configuration">The configuration.</param>
	/// <param name="connector">The connector.</param>
	/// <returns>The configured business machine.</returns>
	public static DaytradeExchangeMachine CreateDaytradeExchange(
		BusinessMachineConfiguration configuration,
		IConnector connector)
	{
		if (configuration == null)
			throw new ArgumentNullException(nameof(configuration));

		if (connector == null)
			throw new ArgumentNullException(nameof(connector));

		// Validate configuration
		var errors = configuration.Validate();
		if (errors.Count > 0)
		{
			throw new ArgumentException($"Configuration validation failed: {string.Join(", ", errors)}");
		}

		// Create machine
		var machine = new DaytradeExchangeMachine
		{
			Connector = connector,
			MaxConcurrentTrades = configuration.MaxConcurrentTrades,
			MaxPositionSize = configuration.MaxPositionSize,
			RiskPercentagePerTrade = configuration.RiskPercentagePerTrade
		};

		// Add trading hours rule if configured
		if (configuration.TradingStartTime.HasValue && configuration.TradingEndTime.HasValue)
		{
			machine.AddRule(new TradingHoursRule(
				configuration.TradingStartTime.Value,
				configuration.TradingEndTime.Value));
		}

		// Add custom rules from configuration
		foreach (var ruleConfig in configuration.Rules)
		{
			if (!ruleConfig.IsEnabled)
				continue;

			var rule = CreateRuleFromConfiguration(ruleConfig);
			if (rule != null)
			{
				machine.AddRule(rule);
			}
		}

		return machine;
	}

	/// <summary>
	/// Creates a business machine with default configuration.
	/// </summary>
	/// <param name="connector">The connector.</param>
	/// <returns>The business machine with default settings.</returns>
	public static DaytradeExchangeMachine CreateDefault(IConnector connector)
	{
		var configuration = BusinessMachineConfiguration.CreateDefault();
		return CreateDaytradeExchange(configuration, connector);
	}

	private static IBusinessRule CreateRuleFromConfiguration(BusinessRuleConfiguration config)
	{
		// This is a simplified implementation
		// In a real system, you might use reflection or a plugin system to create rules dynamically
		return null;
	}
}
