# BusinessMachine Quick Reference

## Getting Started

### 1. Create a Machine
```csharp
using StockSharp.BusinessMachine;
using StockSharp.BusinessEntities;
using StockSharp.Algo;

var connector = new Connector();
var machine = BusinessMachineFactory.CreateDefault(connector);
```

### 2. Subscribe to Events
```csharp
machine.StateChanged += (s, e) => 
    Console.WriteLine($"State: {e.OldState} -> {e.NewState}");

machine.Error += (s, e) => 
    Console.WriteLine($"Error: {e.Exception.Message}");

machine.BusinessRuleViolation += (s, e) => 
    Console.WriteLine($"Rule '{e.RuleName}' violated: {e.Message}");
```

### 3. Start the Machine
```csharp
machine.Start();
```

### 4. Execute Trades
```csharp
var security = connector.LookupById("AAPL@NASDAQ");
var workflowId = machine.ExecuteTrade(security, Sides.Buy, 100);
```

### 5. Monitor Workflow
```csharp
var status = machine.GetWorkflowStatus(workflowId);
Console.WriteLine($"Workflow {workflowId}: {status}");
```

### 6. Clean Up
```csharp
machine.Stop();
machine.Dispose();
```

## Configuration

### Custom Configuration
```csharp
var config = new BusinessMachineConfiguration
{
    MachineName = "MyTrading",
    MaxConcurrentTrades = 5,
    MaxPositionSize = 500,
    RiskPercentagePerTrade = 1.5m,
    TradingStartTime = new TimeSpan(9, 30, 0),
    TradingEndTime = new TimeSpan(16, 0, 0)
};

var machine = BusinessMachineFactory.CreateDaytradeExchange(config, connector);
```

## Custom Business Rules

```csharp
public class CustomVolumeRule : IBusinessRule
{
    public string Name => "CustomVolume";
    public string Description => "Validates volume range";
    
    public ValidationResult Validate(TradeContext context)
    {
        if (context.Volume < 10 || context.Volume > 1000)
            return ValidationResult.Failure("Volume must be 10-1000");
        return ValidationResult.Success();
    }
}

machine.AddRule(new CustomVolumeRule());
```

## Custom Workflow Steps

```csharp
public class CustomNotificationStep : WorkflowStepBase
{
    public CustomNotificationStep() : base("CustomNotification") { }
    
    protected override WorkflowStepResult ExecuteInternal(WorkflowContext context)
    {
        var tradeContext = context.GetData<TradeContext>("TradeContext");
        // Send notification
        Console.WriteLine($"Trade notification: {tradeContext.Security.Id}");
        return WorkflowStepResult.Success();
    }
}
```

## Workflow Engine

```csharp
var engine = new WorkflowEngine();

engine.RegisterWorkflow("CustomTrade",
    new TradeValidationStep(),
    new ComplianceCheckStep(),
    new RiskAssessmentStep(2.0m),
    new OrderPreparationStep(),
    new OrderExecutionStep(connector),
    new CustomNotificationStep(),
    new TradeLoggingStep()
);

var context = new WorkflowContext();
context.SetData("TradeContext", tradeContext);
var result = engine.ExecuteWorkflow("CustomTrade", context);
```

## State Management

### Machine States
- `Initialized`: Machine created, not started
- `Starting`: Initialization in progress
- `Running`: Processing trades
- `Paused`: Temporarily stopped
- `Stopping`: Shutdown in progress
- `Stopped`: Shut down
- `Error`: Error occurred

### Workflow States
- `Pending`: Not yet started
- `Running`: Currently executing
- `Paused`: Temporarily paused
- `Completed`: Successfully finished
- `Cancelled`: User cancelled
- `Failed`: Execution failed

## Built-in Rules

1. **MaxConcurrentTradesRule**: Limits simultaneous trades
2. **MaxPositionSizeRule**: Enforces position size limits
3. **RiskManagementRule**: Validates risk percentage
4. **TradingHoursRule**: Restricts trading hours

## Built-in Workflow Steps

1. **TradeValidationStep**: Validates trade requests
2. **RiskAssessmentStep**: Assesses trade risk
3. **OrderPreparationStep**: Prepares order details
4. **OrderExecutionStep**: Executes orders
5. **PositionMonitoringStep**: Monitors positions
6. **TradeLoggingStep**: Logs trade details
7. **ComplianceCheckStep**: Performs compliance checks

## Common Patterns

### Lifecycle Management
```csharp
try
{
    machine.Start();
    // Do work
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    machine.Stop();
    machine.Dispose();
}
```

### Error Handling
```csharp
try
{
    var workflowId = machine.ExecuteTrade(security, side, volume);
}
catch (BusinessRuleException ex)
{
    Console.WriteLine($"Rule {ex.RuleName} violated");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid operation: {ex.Message}");
}
```

### Workflow Monitoring
```csharp
var workflows = machine.GetActiveWorkflows();
foreach (var workflow in workflows)
{
    Console.WriteLine($"Workflow {workflow.Id}: {workflow.Status}");
}

// Cleanup completed
machine.CleanupCompletedWorkflows();
```

## Best Practices

1. **Always dispose**: Call `Dispose()` when done
2. **Handle events**: Subscribe to state change and error events
3. **Validate config**: Check configuration before creating machine
4. **Monitor workflows**: Regularly check workflow status
5. **Clean up**: Periodically clean completed workflows
6. **Test rules**: Thoroughly test custom business rules
7. **Document steps**: Document custom workflow steps
8. **Handle errors**: Implement proper error handling
9. **Use factory**: Use factory for consistent creation
10. **Configure properly**: Set appropriate limits and thresholds

## Troubleshooting

### Machine Won't Start
- Check connector is configured
- Verify state is Initialized or Stopped
- Check for exception in Error event

### Rule Violations
- Review rule parameters
- Check trade context values
- Subscribe to BusinessRuleViolation event

### Workflow Failures
- Check workflow status
- Review error message
- Verify connector connectivity

### Performance Issues
- Clean up completed workflows
- Check concurrent trade limit
- Monitor system resources

## Resources

- **README.md**: Full documentation
- **ARCHITECTURE.md**: Architecture diagrams
- **INTEGRATION.md**: Integration guide
- **CHANGELOG.md**: Version history
- **Samples**: Example implementations

## API Reference

### Key Classes
- `BusinessMachine`: Base class
- `DaytradeExchangeMachine`: Trading implementation
- `BusinessMachineConfiguration`: Configuration
- `BusinessMachineFactory`: Factory
- `TradeWorkflow`: Workflow tracking
- `WorkflowEngine`: Workflow orchestration

### Key Interfaces
- `IBusinessRule`: Business rule contract
- `IWorkflowStep`: Workflow step contract

### Key Enums
- `BusinessMachineState`: Machine states
- `TradeWorkflowStatus`: Workflow states
- `BusinessMachineOperation`: Operation types

## Support

For questions or issues:
1. Check documentation
2. Review samples
3. Search issue tracker
4. Create new issue if needed
