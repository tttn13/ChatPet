using System.Diagnostics;

namespace PetAssistant.Services;

/// <summary>
/// Simple abstract base class for services providing common logging functionality.
/// This demonstrates a practical use of abstract classes for shared behavior.
/// </summary>
public abstract class BaseService
{
    protected readonly ILogger Logger;
    private readonly string _serviceName;

    protected BaseService(ILogger logger)
    {
        Logger = logger;
        _serviceName = GetType().Name;
    }

    /// <summary>
    /// Log and execute an operation with timing
    /// </summary>
    protected async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            LogOperationStart(operationName);

            var result = await operation();

            LogOperationSuccess(operationName, stopwatch.Elapsed);

            return result;
        }
        catch (Exception ex)
        {
            LogOperationFailure(operationName, stopwatch.Elapsed, ex);
            throw;
        }
    }

    /// <summary>
    /// Log and execute a void operation with timing
    /// </summary>
    protected async Task ExecuteAsync(
        Func<Task> operation,
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            LogOperationStart(operationName);

            await operation();

            LogOperationSuccess(operationName, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            LogOperationFailure(operationName, stopwatch.Elapsed, ex);
            throw;
        }
    }

    /// <summary>
    /// Log operation start
    /// </summary>
    protected virtual void LogOperationStart(string operationName)
    {
        Logger.LogDebug(
            "[{Service}] Starting {Operation}",
            _serviceName, operationName);
    }

    /// <summary>
    /// Log operation success
    /// </summary>
    protected virtual void LogOperationSuccess(string operationName, TimeSpan duration)
    {
        Logger.LogInformation(
            "[{Service}] Completed {Operation} in {Duration}ms",
            _serviceName, operationName, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Log operation failure
    /// </summary>
    protected virtual void LogOperationFailure(string operationName, TimeSpan duration, Exception ex)
    {
        Logger.LogError(ex,
            "[{Service}] Failed {Operation} after {Duration}ms: {Error}",
            _serviceName, operationName, duration.TotalMilliseconds, ex.Message);
    }

    /// <summary>
    /// Log a warning
    /// </summary>
    protected void LogWarning(string message, params object[] args)
    {
        Logger.LogWarning($"[{_serviceName}] {message}", args);
    }

    /// <summary>
    /// Log information
    /// </summary>
    protected void LogInfo(string message, params object[] args)
    {
        Logger.LogInformation($"[{_serviceName}] {message}", args);
    }

    /// <summary>
    /// Log debug information
    /// </summary>
    protected void LogDebug(string message, params object[] args)
    {
        Logger.LogDebug($"[{_serviceName}] {message}", args);
    }
}