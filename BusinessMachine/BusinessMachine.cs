namespace StockSharp.BusinessMachine;

using System;
using System.Collections.Generic;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Base class for business machine implementations.
/// Provides state machine functionality for managing trading business logic.
/// </summary>
public abstract class BusinessMachine : IDisposable
{
	private BusinessMachineState _state = BusinessMachineState.Initialized;
	private readonly object _stateLock = new object();
	private readonly Dictionary<string, object> _stateData = new Dictionary<string, object>();

	/// <summary>
	/// Initializes a new instance of the <see cref="BusinessMachine"/>.
	/// </summary>
	/// <param name="name">The name of the business machine.</param>
	protected BusinessMachine(string name)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
	}

	/// <summary>
	/// Gets the name of the business machine.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the current state of the business machine.
	/// </summary>
	public BusinessMachineState State
	{
		get
		{
			lock (_stateLock)
			{
				return _state;
			}
		}
		protected set
		{
			lock (_stateLock)
			{
				var oldState = _state;
				_state = value;
				OnStateChanged(oldState, value);
			}
		}
	}

	/// <summary>
	/// Event raised when the state changes.
	/// </summary>
	public event EventHandler<StateChangedEventArgs> StateChanged;

	/// <summary>
	/// Event raised when an error occurs.
	/// </summary>
	public event EventHandler<ErrorEventArgs> Error;

	/// <summary>
	/// Event raised when a business rule is violated.
	/// </summary>
	public event EventHandler<BusinessRuleViolationEventArgs> BusinessRuleViolation;

	/// <summary>
	/// Starts the business machine.
	/// </summary>
	public virtual void Start()
	{
		if (State != BusinessMachineState.Initialized && State != BusinessMachineState.Stopped)
			throw new InvalidOperationException($"Cannot start machine from state {State}");

		State = BusinessMachineState.Starting;
		
		try
		{
			OnStart();
			State = BusinessMachineState.Running;
		}
		catch (Exception ex)
		{
			State = BusinessMachineState.Error;
			OnError(ex);
			throw;
		}
	}

	/// <summary>
	/// Stops the business machine.
	/// </summary>
	public virtual void Stop()
	{
		if (State != BusinessMachineState.Running && State != BusinessMachineState.Paused)
			throw new InvalidOperationException($"Cannot stop machine from state {State}");

		State = BusinessMachineState.Stopping;
		
		try
		{
			OnStop();
			State = BusinessMachineState.Stopped;
		}
		catch (Exception ex)
		{
			State = BusinessMachineState.Error;
			OnError(ex);
			throw;
		}
	}

	/// <summary>
	/// Pauses the business machine.
	/// </summary>
	public virtual void Pause()
	{
		if (State != BusinessMachineState.Running)
			throw new InvalidOperationException($"Cannot pause machine from state {State}");

		State = BusinessMachineState.Paused;
		OnPause();
	}

	/// <summary>
	/// Resumes the business machine.
	/// </summary>
	public virtual void Resume()
	{
		if (State != BusinessMachineState.Paused)
			throw new InvalidOperationException($"Cannot resume machine from state {State}");

		State = BusinessMachineState.Running;
		OnResume();
	}

	/// <summary>
	/// Stores state data.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="value">The value.</param>
	protected void SetStateData(string key, object value)
	{
		lock (_stateData)
		{
			_stateData[key] = value;
		}
	}

	/// <summary>
	/// Retrieves state data.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="key">The key.</param>
	/// <returns>The value.</returns>
	protected T GetStateData<T>(string key)
	{
		lock (_stateData)
		{
			return _stateData.TryGetValue(key, out var value) ? (T)value : default;
		}
	}

	/// <summary>
	/// Called when the machine starts.
	/// </summary>
	protected abstract void OnStart();

	/// <summary>
	/// Called when the machine stops.
	/// </summary>
	protected abstract void OnStop();

	/// <summary>
	/// Called when the machine is paused.
	/// </summary>
	protected virtual void OnPause() { }

	/// <summary>
	/// Called when the machine is resumed.
	/// </summary>
	protected virtual void OnResume() { }

	/// <summary>
	/// Called when the state changes.
	/// </summary>
	/// <param name="oldState">The old state.</param>
	/// <param name="newState">The new state.</param>
	protected virtual void OnStateChanged(BusinessMachineState oldState, BusinessMachineState newState)
	{
		StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState));
	}

	/// <summary>
	/// Called when an error occurs.
	/// </summary>
	/// <param name="exception">The exception.</param>
	protected virtual void OnError(Exception exception)
	{
		Error?.Invoke(this, new ErrorEventArgs(exception));
	}

	/// <summary>
	/// Called when a business rule is violated.
	/// </summary>
	/// <param name="ruleName">The rule name.</param>
	/// <param name="message">The message.</param>
	protected virtual void OnBusinessRuleViolation(string ruleName, string message)
	{
		BusinessRuleViolation?.Invoke(this, new BusinessRuleViolationEventArgs(ruleName, message));
	}

	/// <summary>
	/// Disposes the business machine.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes the business machine.
	/// </summary>
	/// <param name="disposing">True if disposing.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (State == BusinessMachineState.Running || State == BusinessMachineState.Paused)
			{
				try
				{
					Stop();
				}
				catch
				{
					// Ignore errors during disposal
				}
			}
		}
	}
}

/// <summary>
/// Event arguments for state changes.
/// </summary>
public class StateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StateChangedEventArgs"/>.
	/// </summary>
	public StateChangedEventArgs(BusinessMachineState oldState, BusinessMachineState newState)
	{
		OldState = oldState;
		NewState = newState;
	}

	/// <summary>
	/// Gets the old state.
	/// </summary>
	public BusinessMachineState OldState { get; }

	/// <summary>
	/// Gets the new state.
	/// </summary>
	public BusinessMachineState NewState { get; }
}

/// <summary>
/// Event arguments for errors.
/// </summary>
public class ErrorEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorEventArgs"/>.
	/// </summary>
	public ErrorEventArgs(Exception exception)
	{
		Exception = exception;
	}

	/// <summary>
	/// Gets the exception.
	/// </summary>
	public Exception Exception { get; }
}

/// <summary>
/// Event arguments for business rule violations.
/// </summary>
public class BusinessRuleViolationEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BusinessRuleViolationEventArgs"/>.
	/// </summary>
	public BusinessRuleViolationEventArgs(string ruleName, string message)
	{
		RuleName = ruleName;
		Message = message;
	}

	/// <summary>
	/// Gets the rule name.
	/// </summary>
	public string RuleName { get; }

	/// <summary>
	/// Gets the message.
	/// </summary>
	public string Message { get; }
}
