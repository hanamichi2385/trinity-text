using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>Keeps the logged exceptions so a failing service call can show its real cause.</summary>
    public sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Errors { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Errors.Add(exception?.ToString() ?? formatter(state, exception));
            }
        }

        public override string ToString() => string.Join(Environment.NewLine, Errors);
    }
}
