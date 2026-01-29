namespace StockSharp.BusinessMachine;

using System;

/// <summary>
/// Interface for business rules.
/// </summary>
public interface IBusinessRule
{
	/// <summary>
	/// Gets the name of the rule.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the description of the rule.
	/// </summary>
	string Description { get; }

	/// <summary>
	/// Validates the trade context against the rule.
	/// </summary>
	/// <param name="context">The trade context.</param>
	/// <returns>The validation result.</returns>
	ValidationResult Validate(TradeContext context);
}

/// <summary>
/// Result of a validation.
/// </summary>
public class ValidationResult
{
	/// <summary>
	/// Gets or sets whether the validation passed.
	/// </summary>
	public bool IsValid { get; set; }

	/// <summary>
	/// Gets or sets the validation message.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// Creates a successful validation result.
	/// </summary>
	public static ValidationResult Success() => new ValidationResult { IsValid = true };

	/// <summary>
	/// Creates a failed validation result.
	/// </summary>
	/// <param name="message">The error message.</param>
	public static ValidationResult Failure(string message) => new ValidationResult { IsValid = false, Message = message };
}

/// <summary>
/// Rule that enforces maximum concurrent trades.
/// </summary>
public class MaxConcurrentTradesRule : IBusinessRule
{
	private readonly Func<int> _maxTradesProvider;
	private readonly Func<int> _currentTradesProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaxConcurrentTradesRule"/>.
	/// </summary>
	public MaxConcurrentTradesRule(Func<int> maxTradesProvider, Func<int> currentTradesProvider)
	{
		_maxTradesProvider = maxTradesProvider;
		_currentTradesProvider = currentTradesProvider;
	}

	/// <inheritdoc />
	public string Name => "MaxConcurrentTrades";

	/// <inheritdoc />
	public string Description => "Enforces maximum number of concurrent trades";

	/// <inheritdoc />
	public ValidationResult Validate(TradeContext context)
	{
		var currentTrades = _currentTradesProvider();
		var maxTrades = _maxTradesProvider();

		if (currentTrades >= maxTrades)
		{
			return ValidationResult.Failure($"Maximum concurrent trades ({maxTrades}) reached. Current: {currentTrades}");
		}

		return ValidationResult.Success();
	}
}

/// <summary>
/// Rule that enforces maximum position size.
/// </summary>
public class MaxPositionSizeRule : IBusinessRule
{
	private readonly Func<decimal> _maxPositionSizeProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaxPositionSizeRule"/>.
	/// </summary>
	public MaxPositionSizeRule(Func<decimal> maxPositionSizeProvider)
	{
		_maxPositionSizeProvider = maxPositionSizeProvider;
	}

	/// <inheritdoc />
	public string Name => "MaxPositionSize";

	/// <inheritdoc />
	public string Description => "Enforces maximum position size per trade";

	/// <inheritdoc />
	public ValidationResult Validate(TradeContext context)
	{
		var maxSize = _maxPositionSizeProvider();

		if (context.Volume > maxSize)
		{
			return ValidationResult.Failure($"Position size ({context.Volume}) exceeds maximum ({maxSize})");
		}

		return ValidationResult.Success();
	}
}

/// <summary>
/// Rule that enforces risk management percentage.
/// </summary>
public class RiskManagementRule : IBusinessRule
{
	private readonly Func<decimal> _riskPercentageProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="RiskManagementRule"/>.
	/// </summary>
	public RiskManagementRule(Func<decimal> riskPercentageProvider)
	{
		_riskPercentageProvider = riskPercentageProvider;
	}

	/// <inheritdoc />
	public string Name => "RiskManagement";

	/// <inheritdoc />
	public string Description => "Enforces risk management percentage per trade";

	/// <inheritdoc />
	public ValidationResult Validate(TradeContext context)
	{
		var riskPercentage = _riskPercentageProvider();

		// Basic validation - can be extended with more sophisticated risk calculations
		if (riskPercentage <= 0 || riskPercentage > 100)
		{
			return ValidationResult.Failure($"Risk percentage must be between 0 and 100. Current: {riskPercentage}");
		}

		// Additional risk checks can be added here
		// For example, checking against portfolio value, calculating potential loss, etc.

		return ValidationResult.Success();
	}
}

/// <summary>
/// Rule that enforces trading hours.
/// </summary>
public class TradingHoursRule : IBusinessRule
{
	private readonly TimeSpan _startTime;
	private readonly TimeSpan _endTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="TradingHoursRule"/>.
	/// </summary>
	public TradingHoursRule(TimeSpan startTime, TimeSpan endTime)
	{
		_startTime = startTime;
		_endTime = endTime;
	}

	/// <inheritdoc />
	public string Name => "TradingHours";

	/// <inheritdoc />
	public string Description => "Enforces trading hours restrictions";

	/// <inheritdoc />
	public ValidationResult Validate(TradeContext context)
	{
		var currentTime = context.RequestTime.TimeOfDay;

		if (currentTime < _startTime || currentTime > _endTime)
		{
			return ValidationResult.Failure($"Trading is only allowed between {_startTime} and {_endTime}. Current time: {currentTime}");
		}

		return ValidationResult.Success();
	}
}
