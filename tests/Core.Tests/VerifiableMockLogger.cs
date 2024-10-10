using System;
using Microsoft.Extensions.Logging;

namespace Core.Tests;

/**
 * Wrapper around ILogger that allows performing mock validations on Log(LogLevel logLevel, string message),
 * which is otherwise not possible because the convenience log methods are all extension methods.
 * See also https://github.com/nsubstitute/NSubstitute/issues/597#issuecomment-653555567
 */
public abstract class VerifiableMockLogger<T> : ILogger<T>
{
    void ILogger.Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter
    ) => Log(logLevel, formatter(state, exception));
    public abstract void Log(LogLevel logLevel, string message);
    public virtual bool IsEnabled(LogLevel logLevel) => true;
    public abstract IDisposable? BeginScope<TState>(TState state) where TState : notnull;
}
