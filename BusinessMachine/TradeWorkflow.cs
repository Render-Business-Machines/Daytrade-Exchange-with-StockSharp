namespace StockSharp.BusinessMachine;

using System;
using System.Threading;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Represents the context for a trade operation.
/// </summary>
public class TradeContext
{
	private static long _nextId = 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeContext"/>.
	/// </summary>
	public TradeContext()
	{
		Id = Interlocked.Increment(ref _nextId);
	}

	/// <summary>
	/// Gets the unique identifier for this context.
	/// </summary>
	public long Id { get; }

	/// <summary>
	/// Gets or sets the security.
	/// </summary>
	public Security Security { get; set; }

	/// <summary>
	/// Gets or sets the side (buy/sell).
	/// </summary>
	public Sides Side { get; set; }

	/// <summary>
	/// Gets or sets the volume.
	/// </summary>
	public decimal Volume { get; set; }

	/// <summary>
	/// Gets or sets the request time.
	/// </summary>
	public DateTimeOffset RequestTime { get; set; }

	/// <summary>
	/// Gets or sets additional data.
	/// </summary>
	public object Tag { get; set; }
}

/// <summary>
/// Status of a trade workflow.
/// </summary>
public enum TradeWorkflowStatus
{
	/// <summary>
	/// Workflow not found.
	/// </summary>
	NotFound,

	/// <summary>
	/// Workflow is pending.
	/// </summary>
	Pending,

	/// <summary>
	/// Workflow is running.
	/// </summary>
	Running,

	/// <summary>
	/// Workflow is paused.
	/// </summary>
	Paused,

	/// <summary>
	/// Workflow completed successfully.
	/// </summary>
	Completed,

	/// <summary>
	/// Workflow was cancelled.
	/// </summary>
	Cancelled,

	/// <summary>
	/// Workflow failed.
	/// </summary>
	Failed
}

/// <summary>
/// Represents a trade workflow.
/// </summary>
public class TradeWorkflow
{
	private TradeWorkflowStatus _status = TradeWorkflowStatus.Pending;
	private readonly object _statusLock = new object();

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeWorkflow"/>.
	/// </summary>
	/// <param name="context">The trade context.</param>
	public TradeWorkflow(TradeContext context)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
		Id = context.Id;
		CreatedTime = DateTimeOffset.Now;
	}

	/// <summary>
	/// Gets the workflow ID.
	/// </summary>
	public long Id { get; }

	/// <summary>
	/// Gets the trade context.
	/// </summary>
	public TradeContext Context { get; }

	/// <summary>
	/// Gets the created time.
	/// </summary>
	public DateTimeOffset CreatedTime { get; }

	/// <summary>
	/// Gets the started time.
	/// </summary>
	public DateTimeOffset? StartedTime { get; private set; }

	/// <summary>
	/// Gets the completed time.
	/// </summary>
	public DateTimeOffset? CompletedTime { get; private set; }

	/// <summary>
	/// Gets the current status.
	/// </summary>
	public TradeWorkflowStatus Status
	{
		get
		{
			lock (_statusLock)
			{
				return _status;
			}
		}
		private set
		{
			lock (_statusLock)
			{
				_status = value;
			}
		}
	}

	/// <summary>
	/// Gets the error message if failed.
	/// </summary>
	public string ErrorMessage { get; private set; }

	/// <summary>
	/// Event raised when the workflow status changes.
	/// </summary>
	public event EventHandler<TradeWorkflowStatusChangedEventArgs> StatusChanged;

	/// <summary>
	/// Starts the workflow.
	/// </summary>
	public void Start()
	{
		if (Status != TradeWorkflowStatus.Pending)
			throw new InvalidOperationException($"Cannot start workflow from status {Status}");

		StartedTime = DateTimeOffset.Now;
		ChangeStatus(TradeWorkflowStatus.Running);
	}

	/// <summary>
	/// Pauses the workflow.
	/// </summary>
	public void Pause()
	{
		if (Status != TradeWorkflowStatus.Running)
			throw new InvalidOperationException($"Cannot pause workflow from status {Status}");

		ChangeStatus(TradeWorkflowStatus.Paused);
	}

	/// <summary>
	/// Resumes the workflow.
	/// </summary>
	public void Resume()
	{
		if (Status != TradeWorkflowStatus.Paused)
			throw new InvalidOperationException($"Cannot resume workflow from status {Status}");

		ChangeStatus(TradeWorkflowStatus.Running);
	}

	/// <summary>
	/// Completes the workflow.
	/// </summary>
	public void Complete()
	{
		if (Status != TradeWorkflowStatus.Running)
			throw new InvalidOperationException($"Cannot complete workflow from status {Status}");

		CompletedTime = DateTimeOffset.Now;
		ChangeStatus(TradeWorkflowStatus.Completed);
	}

	/// <summary>
	/// Cancels the workflow.
	/// </summary>
	public void Cancel()
	{
		if (Status == TradeWorkflowStatus.Completed || Status == TradeWorkflowStatus.Cancelled || Status == TradeWorkflowStatus.Failed)
			return; // Already in a terminal state

		CompletedTime = DateTimeOffset.Now;
		ChangeStatus(TradeWorkflowStatus.Cancelled);
	}

	/// <summary>
	/// Marks the workflow as failed.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	public void Fail(string errorMessage)
	{
		if (Status != TradeWorkflowStatus.Running)
			throw new InvalidOperationException($"Cannot fail workflow from status {Status}");

		ErrorMessage = errorMessage;
		CompletedTime = DateTimeOffset.Now;
		ChangeStatus(TradeWorkflowStatus.Failed);
	}

	private void ChangeStatus(TradeWorkflowStatus newStatus)
	{
		var oldStatus = Status;
		Status = newStatus;
		OnStatusChanged(oldStatus, newStatus);
	}

	/// <summary>
	/// Called when the status changes.
	/// </summary>
	/// <param name="oldStatus">The old status.</param>
	/// <param name="newStatus">The new status.</param>
	protected virtual void OnStatusChanged(TradeWorkflowStatus oldStatus, TradeWorkflowStatus newStatus)
	{
		StatusChanged?.Invoke(this, new TradeWorkflowStatusChangedEventArgs(oldStatus, newStatus));
	}
}

/// <summary>
/// Event arguments for workflow status changes.
/// </summary>
public class TradeWorkflowStatusChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TradeWorkflowStatusChangedEventArgs"/>.
	/// </summary>
	public TradeWorkflowStatusChangedEventArgs(TradeWorkflowStatus oldStatus, TradeWorkflowStatus newStatus)
	{
		OldStatus = oldStatus;
		NewStatus = newStatus;
	}

	/// <summary>
	/// Gets the old status.
	/// </summary>
	public TradeWorkflowStatus OldStatus { get; }

	/// <summary>
	/// Gets the new status.
	/// </summary>
	public TradeWorkflowStatus NewStatus { get; }
}
