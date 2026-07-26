using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NiveraAPI.Attributes;
using NiveraAPI.Console;

namespace NiveraAPI.Utilities;

public static class Exceptions
{
	private static List<Exception> _exceptionStore = new();
	private static List<Exception> _unhandledExceptionStore = new();

	/// <summary>
	/// Provides an immutable collection of exceptions that have been captured by the application.
	/// </summary>
	/// <remarks>
	/// This property exposes a read-only list of exceptions that have been logged or handled
	/// by the exception management system. It can be used to review or debug previously
	/// caught exceptions without altering the underlying collection.
	/// </remarks>
	/// <value>
	/// A read-only list of <see cref="System.Exception"/> objects, representing all logged exceptions.
	/// </value>
	public static IReadOnlyList<Exception> ExceptionStore => _exceptionStore;

	/// <summary>
	/// Provides an immutable collection of exceptions that were not handled by the application.
	/// </summary>
	/// <remarks>
	/// This property contains a read-only list of exceptions that triggered but went unhandled during
	/// the application's execution. It is useful for debugging, post-mortem analysis, and identifying
	/// critical issues that need to be addressed to improve application stability.
	/// </remarks>
	/// <value>
	/// A read-only list of <see cref="System.Exception"/> objects representing all unhandled exceptions
	/// captured by the exception logging system.
	/// </value>
	public static IReadOnlyList<Exception> UnhandledExceptionStore => _unhandledExceptionStore;

	/// <summary>
	/// Retrieves the most recent exception captured within the exception management system.
	/// </summary>
	/// <remarks>
	/// This property provides access to the last exception that was logged or handled. It can be useful
	/// for debugging purposes or for monitoring the latest error encountered by the application.
	/// Note that this value is overwritten each time a new exception is logged, and does not persist
	/// unhandled exceptions separately.
	/// </remarks>
	/// <value>
	/// An instance of <see cref="System.Exception"/> representing the most recent exception
	/// captured by the system. If no exception has been recorded, this property may be null.
	/// </value>
	public static Exception? LastException { get; private set; }

	/// <summary>
	/// Represents the most recent unhandled exception that has occurred in the application.
	/// </summary>
	/// <remarks>
	/// This property stores the last exception that was not caught or handled by the application.
	/// It can be used for debugging purposes to identify critical errors that require attention.
	/// This value is updated whenever an unhandled exception is logged, and it is overwritten by
	/// subsequent unhandled exceptions.
	/// </remarks>
	/// <value>
	/// An instance of <see cref="System.Exception"/> representing the last unhandled exception,
	/// or <c>null</c> if no unhandled exceptions have been recorded.
	/// </value>
	public static Exception? LastUnhandledException { get; private set; }

	/// <summary>
	/// Provides a snapshot of the current execution stack.
	/// </summary>
	/// <remarks>
	/// This property generates and returns a new <see cref="System.Diagnostics.StackTrace"/>
	/// object representing the current state of the application's call stack. It can be used
	/// for debugging or logging purposes to analyze the method invocations leading to the current
	/// point in the program.
	/// </remarks>
	/// <value>
	/// A <see cref="System.Diagnostics.StackTrace"/> instance containing structured stack trace
	/// information for the current context.
	/// </value>
	public static StackTrace Trace => new StackTrace();

	/// <summary>
	/// Represents a collection of stack frames captured from the current execution context of the application.
	/// </summary>
	/// <remarks>
	/// This property provides a read-only list of stack frames, allowing inspection of the sequence of method calls
	/// that led to the current state. It filters and organizes the stack trace of the executing application, offering
	/// valuable debugging information without modifying the original stack data.
	/// </remarks>
	/// <value>
	/// A read-only list of <see cref="System.Diagnostics.StackFrame"/> objects that describe the methods in the call stack.
	/// </value>
	public static List<StackFrame> Frames
	{
		get
		{
			return Trace.GetFrames().Where(f =>
			{
				var type = f.GetMethod().DeclaringType;
				return type == null || type != typeof(Exceptions);
			}).ToList();
		}
	}

	/// <summary>
	/// Retrieves a collection of all method metadata objects from the current stack trace.
	/// </summary>
	/// <remarks>
	/// This property provides a read-only list of methods that are part of the stack trace at the time of invocation.
	/// Each method is represented as a <see cref="System.Reflection.MethodBase"/> object,
	/// which includes information such as the method name, parameters, and declaring type.
	/// This can be useful for debugging, logging, or analyzing the flow of a program at runtime.
	/// </remarks>
	/// <value>
	/// A read-only list of <see cref="System.Reflection.MethodBase"/> objects, representing
	/// the methods present in the current stack trace.
	/// </value>
	public static List<MethodBase> StackMethods => Frames.Select(x => x.GetMethod()).ToList();

	/// <summary>
	/// Retrieves a list of types associated with methods in the current stack trace.
	/// </summary>
	/// <remarks>
	/// This property examines the stack trace and extracts types from the methods
	/// currently present in the call stack. It provides a detailed view of the
	/// declaring types for all methods involved in the execution flow, which can
	/// be useful for debugging or analyzing the application's runtime behavior.
	/// </remarks>
	/// <value>
	/// A list of <see cref="System.Type"/> objects representing the declaring types
	/// of all methods in the current stack trace. If no methods are found in the stack,
	/// this property returns an empty list.
	/// </value>
	public static List<Type?> StackTypes => StackMethods.Select(x => x.DeclaringType ?? null).ToList();

	/// <summary>
	/// Specifies the file path where exception logs are stored.
	/// </summary>
	/// <value>
	/// A string representing the absolute or relative file path to the exception log file.
	/// </value>
	public static string LogPath { get; set; } = string.Empty;

	/// <summary>
	/// Specifies the file path where unhandled exceptions are logged by the application.
	/// </summary>
	/// <value>
	/// A string representing the full file path to the log file for unhandled exceptions.
	/// </value>
	public static string UnhandledLogPath { get; set; } = string.Empty;

	/// <summary>
	/// Determines whether all captured exceptions should be logged to the configured exception log file.
	/// </summary>
	/// <remarks>
	/// When enabled, every exception encountered by the application is appended to the log file specified
	/// by the <see cref="LogPath"/> property. This also applies even if more targeted logging behavior is
	/// implemented elsewhere. Disabling this property will limit logging to explicitly specified operations
	/// or unhandled exceptions.
	/// </remarks>
	/// <value>
	/// A boolean value indicating whether all exceptions should be logged (<see langword="true"/>),
	/// or only specific or unhandled exceptions (<see langword="false"/>).
	/// </value>
	public static bool LogAll { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether all unhandled exceptions should be logged.
	/// </summary>
	/// <value>
	/// A boolean value where <c>true</c> indicates that all unhandled exceptions will be logged,
	/// and <c>false</c> indicates that only the last unhandled exception will be logged.
	/// </value>
	public static bool UnhandledLogAll { get; set; }

	/// <summary>
	/// Determines whether exceptions are logged to the console when they are captured.
	/// </summary>
	/// <remarks>
	/// This property controls the behavior of console logging for exceptions.
	/// When set to <c>true</c>, all captured exceptions are output to the console in addition
	/// to being written to the log files if logging is enabled. This allows for real-time
	/// monitoring of exceptions during development or troubleshooting.
	/// </remarks>
	/// <value>
	/// A boolean value indicating whether exceptions are logged to the console.
	/// The default value is <c>true</c>.
	/// </value>
	public static bool LogToConsole { get; set; }

	/// <summary>
	/// Occurs when an exception is thrown in the application.
	/// </summary>
	/// <remarks>
	/// This event is triggered whenever any exception is captured by the application's
	/// exception handling mechanisms. It provides a way to monitor or react to exceptions
	/// as they occur in real-time. Subscribers to this event can implement custom logic
	/// such as logging, alerting, or additional exception handling.
	/// </remarks>
	/// <event>
	/// An <see cref="System.Action{T}"/> where T is <see cref="System.Exception"/>, representing the action
	/// to be executed when an exception is thrown.
	/// </event>
	public static event Action<Exception>? Thrown;

	/// <summary>
	/// Represents an event that is triggered when an unhandled exception occurs in the application.
	/// </summary>
	/// <remarks>
	/// This event allows subscribers to be notified of exceptions that are not caught by the application
	/// and are passed to the runtime's unhandled exception handler. It can be used to log, handle,
	/// or process these exceptions to improve application reliability or provide additional debugging
	/// information.
	/// </remarks>
	/// <example>
	/// This member does not include example usage.
	/// </example>
	public static event Action<Exception>? Unhandled;

	/// <summary>
	/// Determines whether any exceptions of the specified type exist in the exception store.
	/// </summary>
	/// <typeparam name="T">The type of exception to check for.</typeparam>
	/// <returns>True if any exceptions of the specified type exist in the exception store; otherwise, false.</returns>
	public static bool Any<T>() where T : Exception
		=> _exceptionStore.Any(x => x is T);

	/// <summary>
	/// Clears all stored exceptions from both the exception store and the unhandled exception store.
	/// </summary>
	public static void Clear()
	{
		_exceptionStore.Clear();
		_unhandledExceptionStore.Clear();
	}
	
		/// <summary>
	/// Logs the provided exception to the appropriate output based on the specified configuration.
	/// </summary>
	/// <param name="isUnhandled">Indicates whether the exception is unhandled.</param>
	/// <param name="exception">The exception instance to log.</param>
	public static void Log(bool isUnhandled, Exception exception)
	{
		try
		{
			var text = ExceptionToString(exception, isTop: true);
			
			if (LogToConsole)
				ConsoleOutput.Write(text, ConsoleColor.DarkRed);
			
			if (isUnhandled)
			{
				if (UnhandledLogAll)
				{
					if (!File.Exists(UnhandledLogPath))
					{
						File.WriteAllText(UnhandledLogPath, text);
					}
					else
					{
						File.AppendAllText(UnhandledLogPath, text);
					}
				}
				else
				{
					File.WriteAllText(UnhandledLogPath, text);
				}
			}
			else if (LogAll)
			{
				if (!File.Exists(LogPath))
				{
					File.WriteAllText(LogPath, text);
				}
				else
				{
					File.AppendAllText(LogPath, text);
				}
			}
			else
			{
				File.WriteAllText(LogPath, text);
			}
		}
		catch
		{
			 // ignored
		}
	}

	/// <summary>
	/// Converts the specified exception, including its details and stack trace, into a formatted string representation.
	/// </summary>
	/// <param name="exception">The exception to convert to a string.</param>
	/// <param name="isTop">A boolean value indicating whether this is the top-level exception being logged. If true, additional formatting will be included.</param>
	/// <returns>A formatted string representation of the exception, including details such as the type, message, source, and stack trace.</returns>
	public static string ExceptionToString(Exception exception, bool isTop)
	{
		var stringBuilder = Pools.PoolStringBuilder();

		if (isTop)
		{
			stringBuilder.AppendLine("---- Exception thrown at " + DateTime.Now.ToString("G") + " ----");
		}
		try
		{
			stringBuilder.AppendLine($"Type: {exception.GetType().FullName}\nResult: {exception.HResult}\nSource: {exception.Source ?? "Unknown"}\nMessage: {exception.Message}");
		}
		catch
		{
			 // ignored
		}
		
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Stack Trace:");
		stringBuilder.AppendLine(StackToString(Trace, includeProcess: true));
		
		if (isTop)
		{
			stringBuilder.AppendLine("------------------------------");
			stringBuilder.AppendLine();
		}

		return Pools.ReturnStringBuilderValue(stringBuilder);
	}

	/// <summary>
	/// Converts the specified stack trace into a formatted string representation.
	/// </summary>
	/// <param name="trace">The stack trace to be formatted.</param>
	/// <param name="includeProcess">A boolean value indicating whether process and thread information should be included in the output.</param>
	/// <returns>A formatted string representation of the stack trace.</returns>
	public static string StackToString(StackTrace trace, bool includeProcess)
	{
		var stringBuilder = Pools.PoolStringBuilder();
		var frames = trace.GetFrames();
		
		stringBuilder.AppendLine($"---- Stack start: {frames.Length} frames -----");
		
		if (includeProcess)
		{
			try
			{
				var currentProcess = Process.GetCurrentProcess();
				var currentThread = Thread.CurrentThread;
				
				stringBuilder.AppendLine($"-> Process Information <-\n  >- ID: {currentProcess.Id}\n  >- Name: {currentProcess.ProcessName}\n  -> Memory: {currentProcess.WorkingSet64}\n-----------------------");
				stringBuilder.AppendLine();
				
				stringBuilder.AppendLine($"-> Thread Information <-\n  >- ID: {currentThread.ManagedThreadId}\n  -> Name: {currentThread.Name}\n  -> Priority: {currentThread.Priority}\n-------------------------");
				stringBuilder.AppendLine();
			}
			catch
			{
				 // ignored
			}
		}
		
		for (var i = 0; i < frames.Length; i++)
		{
			var stackFrame = frames[i];
			var methodBase = stackFrame.GetMethod();
			var declaringType = methodBase.DeclaringType;
			var assembly = declaringType?.Assembly;
			
			try
			{
				stringBuilder.AppendLine($"-> Frame: {i + 1} / {frames.Length}\n" +
				                         $"  >- Method: {methodBase.Name}\n" +
				                         $"  >- Module: {methodBase.Module.Name}\n" +
				                         $"  >- Type: {declaringType?.FullName ?? "no declaring type"}\n" +
				                         $"  >- Assembly: {assembly?.FullName ?? "no assembly"}\n" +
				                         $"  >- Assembly Path: {assembly?.CodeBase ?? "no assembly"}");
			}
			catch
			{
				// ignored
			}
			
			stringBuilder.AppendLine();
		}
		
		stringBuilder.AppendLine("---- Stack end ----");
		return Pools.ReturnStringBuilderValue(stringBuilder);
	}
	
	[Load]
	private static void Load()
	{
		if (AppDomain.CurrentDomain != null)
		{
			AppDomain.CurrentDomain.FirstChanceException += OnException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		LogAll = true;
		LogToConsole = true;
		UnhandledLogAll = true;
		
		LogPath = Path.Combine(Directory.GetCurrentDirectory(), "exception_log.txt");
		UnhandledLogPath = Path.Combine(Directory.GetCurrentDirectory(), "unhandled_exception_log.txt");
	}

	[Unload]
	private static void Unload()
	{
		if (AppDomain.CurrentDomain != null)
		{
			AppDomain.CurrentDomain.FirstChanceException -= OnException;
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
			
			_exceptionStore.Clear();
			_unhandledExceptionStore.Clear();
		}
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs ev)
	{
		if (ev.ExceptionObject != null && ev.ExceptionObject is Exception ex)
		{
			LastUnhandledException = ex;
			
			_unhandledExceptionStore.Add(ex);
			
			try
			{
				Unhandled?.Invoke(ex);
			}
			catch
			{
				// ignored
			}
			
			Log(true, ex);
		}
	}

	private static void OnException(object sender, FirstChanceExceptionEventArgs ev)
	{
		LastException = ev.Exception;
		
		_exceptionStore.Add(ev.Exception);
		
		try
		{
			Thrown?.Invoke(ev.Exception);
		}
		catch
		{
			// ignored
		}
		
		Log(false, ev.Exception);
	}
}