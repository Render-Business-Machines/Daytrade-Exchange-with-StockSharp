# Business Machine Module

## Overview

The Business Machine module provides a robust, state-machine-based framework for managing day trading exchange operations in StockSharp. It implements a workflow-driven approach to trading operations, ensuring business rules are enforced and operations are traceable.

## Key Features

- **State Machine Architecture**: Manages machine lifecycle with well-defined states (Initialized, Starting, Running, Paused, Stopping, Stopped, Error)
- **Business Rules Engine**: Validates trading operations against configurable business rules
- **Workflow System**: Event-driven workflow engine for complex trading operations
- **Trade Workflow Management**: Tracks and manages individual trade workflows from initiation to completion
- **Configuration System**: Flexible configuration for business rules, workflows, and machine settings
- **Risk Management**: Built-in risk assessment and position sizing rules

## Core Components

### 1. BusinessMachine Base Class

Abstract base class that provides state management, event handling, and lifecycle management for all business machines.

```csharp
public abstract class BusinessMachine : IDisposable
{
    public string Name { get; }
    public BusinessMachineState State { get; }
    
    public virtual void Start();
    public virtual void Stop();
    public virtual void Pause();
    public virtual void Resume();
}
```

### 2. DaytradeExchangeMachine

Concrete implementation for day trading exchange operations.

```csharp
public class DaytradeExchangeMachine : BusinessMachine
{
    public IConnector Connector { get; set; }
    public int MaxConcurrentTrades { get; set; }
    public decimal MaxPositionSize { get; set; }
    public decimal RiskPercentagePerTrade { get; set; }
    
    public long ExecuteTrade(Security security, Sides side, decimal volume);
    public TradeWorkflowStatus GetWorkflowStatus(long workflowId);
    public void CancelWorkflow(long workflowId);
}
```

### 3. Business Rules System

Defines and enforces business rules for trading operations.

**Built-in Rules:**
- `MaxConcurrentTradesRule`: Limits the number of simultaneous trades
- `MaxPositionSizeRule`: Enforces maximum position size per trade
- `RiskManagementRule`: Validates risk percentage
- `TradingHoursRule`: Restricts trading to specific hours

```csharp
public interface IBusinessRule
{
    string Name { get; }
    string Description { get; }
    ValidationResult Validate(TradeContext context);
}
```

### 4. Workflow Engine

Event-driven workflow system for orchestrating complex operations.

```csharp
public class WorkflowEngine
{
    public void RegisterWorkflow(string workflowName, params IWorkflowStep[] steps);
    public WorkflowExecutionResult ExecuteWorkflow(string workflowName, WorkflowContext context);
}
```

**Built-in Workflow Steps:**
- `TradeValidationStep`: Validates trade requests
- `RiskAssessmentStep`: Assesses trade risk
- `OrderPreparationStep`: Prepares order details
- `OrderExecutionStep`: Executes the order
- `PositionMonitoringStep`: Monitors position status
- `TradeLoggingStep`: Logs trade details
- `ComplianceCheckStep`: Performs compliance checks

### 5. Trade Workflow

Manages individual trade operations through their lifecycle.

```csharp
public class TradeWorkflow
{
    public long Id { get; }
    public TradeContext Context { get; }
    public TradeWorkflowStatus Status { get; }
    
    public void Start();
    public void Pause();
    public void Resume();
    public void Complete();
    public void Cancel();
    public void Fail(string errorMessage);
}
```

## Usage Examples

### Basic Usage

```csharp
using StockSharp.BusinessMachine;
using StockSharp.BusinessEntities;
using StockSharp.Algo;

// Create a connector
var connector = new Connector();
// ... configure connector ...

// Create a business machine with default configuration
var machine = BusinessMachineFactory.CreateDefault(connector);

// Start the machine
machine.Start();

// Execute a trade
var security = connector.LookupById("AAPL@NASDAQ");
var workflowId = machine.ExecuteTrade(security, Sides.Buy, 100);

// Check workflow status
var status = machine.GetWorkflowStatus(workflowId);

// Stop the machine when done
machine.Stop();
```

### Custom Configuration

```csharp
// Create custom configuration
var config = new BusinessMachineConfiguration
{
    MachineName = "MyDaytradeExchange",
    MaxConcurrentTrades = 5,
    MaxPositionSize = 500,
    RiskPercentagePerTrade = 1.5m,
    TradingStartTime = new TimeSpan(9, 30, 0),
    TradingEndTime = new TimeSpan(16, 0, 0),
    EnableAutoCleanup = true
};

// Create machine with custom configuration
var machine = BusinessMachineFactory.CreateDaytradeExchange(config, connector);
```

### Custom Business Rules

```csharp
// Create a custom rule
public class CustomVolumeRule : IBusinessRule
{
    public string Name => "CustomVolume";
    public string Description => "Custom volume validation";
    
    public ValidationResult Validate(TradeContext context)
    {
        if (context.Volume < 10 || context.Volume > 1000)
            return ValidationResult.Failure("Volume must be between 10 and 1000");
        
        return ValidationResult.Success();
    }
}

// Add custom rule to machine
machine.AddRule(new CustomVolumeRule());
```

### Event Handling

```csharp
// Subscribe to machine events
machine.StateChanged += (sender, e) =>
{
    Console.WriteLine($"State changed from {e.OldState} to {e.NewState}");
};

machine.Error += (sender, e) =>
{
    Console.WriteLine($"Error occurred: {e.Exception.Message}");
};

machine.BusinessRuleViolation += (sender, e) =>
{
    Console.WriteLine($"Rule '{e.RuleName}' violated: {e.Message}");
};
```

### Custom Workflow

```csharp
// Create a workflow engine
var workflowEngine = new WorkflowEngine();

// Register a custom workflow
workflowEngine.RegisterWorkflow("CustomTradeWorkflow",
    new TradeValidationStep(),
    new ComplianceCheckStep(),
    new RiskAssessmentStep(2.0m),
    new OrderPreparationStep(),
    new OrderExecutionStep(connector),
    new PositionMonitoringStep(),
    new TradeLoggingStep()
);

// Execute the workflow
var context = new WorkflowContext();
context.SetData("TradeContext", tradeContext);
var result = workflowEngine.ExecuteWorkflow("CustomTradeWorkflow", context);

if (result.IsSuccess)
{
    Console.WriteLine("Workflow completed successfully");
}
else
{
    Console.WriteLine($"Workflow failed: {result.ErrorMessage}");
}
```

## Architecture

The Business Machine module follows a layered architecture:

1. **State Management Layer**: Manages machine state transitions and lifecycle
2. **Business Rules Layer**: Validates operations against business rules
3. **Workflow Layer**: Orchestrates complex multi-step operations
4. **Integration Layer**: Integrates with StockSharp connectors and entities

## State Machine Diagram

```
Initialized --> Starting --> Running <--> Paused
                    |           |
                    v           v
                  Error      Stopping --> Stopped
```

## Best Practices

1. **Always validate configuration** before creating a machine
2. **Handle events** to monitor machine state and errors
3. **Use custom rules** to enforce business-specific requirements
4. **Leverage workflows** for complex multi-step operations
5. **Clean up completed workflows** periodically to free resources
6. **Stop machines gracefully** before application shutdown
7. **Test business rules** thoroughly before production use

## Thread Safety

The Business Machine module is designed to be thread-safe:
- State changes are protected by locks
- Workflow collections use synchronization
- Configuration is immutable after machine creation

## Performance Considerations

- Workflow cleanup should be performed periodically to prevent memory leaks
- Business rules should be efficient as they run on every trade
- Event handlers should be non-blocking
- Consider using async patterns for I/O-bound operations

## Integration with StockSharp

The Business Machine integrates seamlessly with StockSharp:
- Uses `IConnector` for market operations
- Works with `Security`, `Order`, and other StockSharp entities
- Compatible with all StockSharp connectors
- Leverages StockSharp messages and events

## Future Enhancements

Potential future enhancements:
- Async workflow execution
- Workflow persistence and recovery
- Advanced analytics and reporting
- Machine learning integration for rule optimization
- Multi-machine orchestration
- Distributed workflow execution

## License

This module is part of the StockSharp trading platform and follows the same license terms.
