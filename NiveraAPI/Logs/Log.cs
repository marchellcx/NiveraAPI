using NiveraAPI.Utilities;

namespace NiveraAPI.Logs;

/// <summary>
/// Provides static methods for logging messages of various severities
/// (Debug, Info, Warn, and Error) to a log sink. Includes support for
/// capturing contextual information such as the calling method's name
/// and assembly details.
/// </summary>
public static class Log
{
    private static readonly LogSink sink = new("DefaultCategory", "DefaultName");

    /// <summary>
    /// Logs a debug message to the log sink with additional contextual information,
    /// including the calling method's name and assembly details.
    /// </summary>
    /// <param name="msg">The debug message to be logged. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="msg"/> parameter is null.</exception>
    public static void Debug(object msg, bool enableDebug)
    {
        if (msg == null)
            throw new ArgumentNullException(nameof(msg));

        var sourceCategory = "UnknownAsm";
        var sourceName = "UnknownMethod";
        
        var method = ReflectionHelper.GetCallerMethod(2, false,
            m => m.DeclaringType != null && m.DeclaringType != typeof(Log) &&
                 m.DeclaringType != typeof(ReflectionHelper));


        if (method != null)
        {
            var asmName = method.DeclaringType.Assembly.GetName();

            sourceName = method.DeclaringType.Name;
            sourceCategory = asmName?.Name ?? method.DeclaringType.Assembly.FullName;
        }

        sink.Name = sourceName;
        sink.Category = sourceCategory;

        if (method != null)
        {
            sink.DebugIf(method.Name, msg, enableDebug);
        }
        else
        {
            sink.DebugIf(msg, enableDebug);
        }
    }

    /// <summary>
    /// Logs an informational message to the log sink with additional contextual information,
    /// including the calling method's name and assembly details.
    /// </summary>
    /// <param name="msg">The informational message to be logged. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="msg"/> parameter is null.</exception>
    public static void Info(object msg)
    {
        if (msg == null)
            throw new ArgumentNullException(nameof(msg));

        var sourceCategory = "UnknownAsm";
        var sourceName = "UnknownMethod";
        
        var method = ReflectionHelper.GetCallerMethod(0, false,
            m => m.DeclaringType != null && m.DeclaringType != typeof(Log) &&
                 m.DeclaringType != typeof(ReflectionHelper));


        if (method != null)
        {
            var asmName = method.DeclaringType.Assembly.GetName();

            sourceName = method.DeclaringType.Name;
            sourceCategory = asmName?.Name ?? method.DeclaringType.Assembly.FullName;
        }

        sink.Name = sourceName;
        sink.Category = sourceCategory;

        if (method != null)
        {
            sink.Info(method.Name, msg);
        }
        else
        {
            sink.Info(msg);
        }
    }

    /// <summary>
    /// Logs a warning message to the log sink with additional contextual information,
    /// including the calling method's name and assembly details.
    /// </summary>
    /// <param name="msg">The warning message to be logged. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="msg"/> parameter is null.</exception>
    public static void Warn(object msg)
    {
        if (msg == null)
            throw new ArgumentNullException(nameof(msg));

        var sourceCategory = "UnknownAsm";
        var sourceName = "UnknownMethod";
        
        var method = ReflectionHelper.GetCallerMethod(0, false,
            m => m.DeclaringType != null && m.DeclaringType != typeof(Log) &&
                 m.DeclaringType != typeof(ReflectionHelper));


        if (method != null)
        {
            var asmName = method.DeclaringType.Assembly.GetName();

            sourceName = method.DeclaringType.Name;
            sourceCategory = asmName?.Name ?? method.DeclaringType.Assembly.FullName;
        }

        sink.Name = sourceName;
        sink.Category = sourceCategory;

        if (method != null)
        {
            sink.Warn(method.Name, msg);
        }
        else
        {
            sink.Warn(msg);
        }
    }

    /// <summary>
    /// Logs an error message to the log sink with additional contextual information
    /// such as the caller's method name and assembly.
    /// </summary>
    /// <param name="msg">The message to log. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="msg"/> parameter is null.</exception>
    public static void Error(object msg)
    {
        if (msg == null)
            throw new ArgumentNullException(nameof(msg));

        var sourceCategory = "UnknownAsm";
        var sourceName = "UnknownMethod";
        
        var method = ReflectionHelper.GetCallerMethod(0, false,
            m => m.DeclaringType != null && m.DeclaringType != typeof(Log) &&
                 m.DeclaringType != typeof(ReflectionHelper));


        if (method != null)
        {
            var asmName = method.DeclaringType.Assembly.GetName();

            sourceName = method.DeclaringType.Name;
            sourceCategory = asmName?.Name ?? method.DeclaringType.Assembly.FullName;
        }

        sink.Name = sourceName;
        sink.Category = sourceCategory;

        if (method != null)
        {
            sink.Error(method.Name, msg);
        }
        else
        {
            sink.Error(msg);
        }
    }
}