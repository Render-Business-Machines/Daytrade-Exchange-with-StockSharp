namespace StockSharp.BusinessMachine;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Event-driven workflow engine for business machine operations.
/// </summary>
public class WorkflowEngine
{
	private readonly Dictionary<string, List<IWorkflowStep>> _workflows = new Dictionary<string, List<IWorkflowStep>>();
	private readonly object _workflowLock = new object();

	/// <summary>
	/// Registers a workflow.
	/// </summary>
	/// <param name="workflowName">The workflow name.</param>
	/// <param name="steps">The workflow steps.</param>
	public void RegisterWorkflow(string workflowName, params IWorkflowStep[] steps)
	{
		if (string.IsNullOrWhiteSpace(workflowName))
			throw new ArgumentException("Workflow name cannot be null or empty", nameof(workflowName));

		if (steps == null || steps.Length == 0)
			throw new ArgumentException("Workflow must have at least one step", nameof(steps));

		lock (_workflowLock)
		{
			_workflows[workflowName] = new List<IWorkflowStep>(steps);
		}
	}

	/// <summary>
	/// Executes a workflow.
	/// </summary>
	/// <param name="workflowName">The workflow name.</param>
	/// <param name="context">The execution context.</param>
	/// <returns>The workflow execution result.</returns>
	public WorkflowExecutionResult ExecuteWorkflow(string workflowName, WorkflowContext context)
	{
		List<IWorkflowStep> steps;
		lock (_workflowLock)
		{
			if (!_workflows.TryGetValue(workflowName, out steps))
			{
				return WorkflowExecutionResult.Failure($"Workflow '{workflowName}' not found");
			}
		}

		var result = new WorkflowExecutionResult { IsSuccess = true };

		try
		{
			foreach (var step in steps)
			{
				var stepResult = step.Execute(context);
				result.StepResults.Add(stepResult);

				if (!stepResult.IsSuccess)
				{
					result.IsSuccess = false;
					result.ErrorMessage = stepResult.ErrorMessage;
					break;
				}

				// Check if workflow should continue
				if (stepResult.ShouldTerminate)
				{
					break;
				}
			}
		}
		catch (Exception ex)
		{
			result.IsSuccess = false;
			result.ErrorMessage = ex.Message;
			result.Exception = ex;
		}

		return result;
	}

	/// <summary>
	/// Gets all registered workflows.
	/// </summary>
	/// <returns>The workflow names.</returns>
	public IEnumerable<string> GetRegisteredWorkflows()
	{
		lock (_workflowLock)
		{
			return _workflows.Keys.ToList();
		}
	}
}

/// <summary>
/// Interface for workflow steps.
/// </summary>
public interface IWorkflowStep
{
	/// <summary>
	/// Gets the name of the step.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Executes the step.
	/// </summary>
	/// <param name="context">The workflow context.</param>
	/// <returns>The step result.</returns>
	WorkflowStepResult Execute(WorkflowContext context);
}

/// <summary>
/// Context for workflow execution.
/// </summary>
public class WorkflowContext
{
	private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

	/// <summary>
	/// Sets data in the context.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="value">The value.</param>
	public void SetData(string key, object value)
	{
		_data[key] = value;
	}

	/// <summary>
	/// Gets data from the context.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="key">The key.</param>
	/// <returns>The value.</returns>
	public T GetData<T>(string key)
	{
		return _data.TryGetValue(key, out var value) ? (T)value : default;
	}

	/// <summary>
	/// Checks if data exists in the context.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>True if the data exists.</returns>
	public bool HasData(string key)
	{
		return _data.ContainsKey(key);
	}
}

/// <summary>
/// Result of a workflow execution.
/// </summary>
public class WorkflowExecutionResult
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WorkflowExecutionResult"/>.
	/// </summary>
	public WorkflowExecutionResult()
	{
		StepResults = new List<WorkflowStepResult>();
	}

	/// <summary>
	/// Gets or sets whether the workflow succeeded.
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	/// Gets or sets the error message.
	/// </summary>
	public string ErrorMessage { get; set; }

	/// <summary>
	/// Gets or sets the exception if any.
	/// </summary>
	public Exception Exception { get; set; }

	/// <summary>
	/// Gets the step results.
	/// </summary>
	public List<WorkflowStepResult> StepResults { get; }

	/// <summary>
	/// Creates a successful result.
	/// </summary>
	public static WorkflowExecutionResult Success() => new WorkflowExecutionResult { IsSuccess = true };

	/// <summary>
	/// Creates a failed result.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	public static WorkflowExecutionResult Failure(string errorMessage) => 
		new WorkflowExecutionResult { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of a workflow step execution.
/// </summary>
public class WorkflowStepResult
{
	/// <summary>
	/// Gets or sets whether the step succeeded.
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	/// Gets or sets the error message.
	/// </summary>
	public string ErrorMessage { get; set; }

	/// <summary>
	/// Gets or sets whether the workflow should terminate after this step.
	/// </summary>
	public bool ShouldTerminate { get; set; }

	/// <summary>
	/// Creates a successful result.
	/// </summary>
	public static WorkflowStepResult Success() => new WorkflowStepResult { IsSuccess = true };

	/// <summary>
	/// Creates a failed result.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	public static WorkflowStepResult Failure(string errorMessage) => 
		new WorkflowStepResult { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Base class for workflow steps.
/// </summary>
public abstract class WorkflowStepBase : IWorkflowStep
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WorkflowStepBase"/>.
	/// </summary>
	/// <param name="name">The step name.</param>
	protected WorkflowStepBase(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	/// <inheritdoc />
	public string Name { get; }

	/// <inheritdoc />
	public WorkflowStepResult Execute(WorkflowContext context)
	{
		try
		{
			return ExecuteInternal(context);
		}
		catch (Exception ex)
		{
			return WorkflowStepResult.Failure($"Step '{Name}' failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Executes the step logic.
	/// </summary>
	/// <param name="context">The workflow context.</param>
	/// <returns>The step result.</returns>
	protected abstract WorkflowStepResult ExecuteInternal(WorkflowContext context);
}
