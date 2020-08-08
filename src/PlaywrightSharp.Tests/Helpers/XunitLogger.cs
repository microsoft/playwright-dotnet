using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Helpers
{
    internal class XunitLogger : ILogger, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private bool _enabled = true;

        public XunitLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_enabled)
            {
                try
                {
                    _output.WriteLine(state.ToString());
                }
                catch { }
            }
        }

        public bool IsEnabled(LogLevel logLevel) => _enabled;

        public IDisposable BeginScope<TState>(TState state) => this;

        public void Dispose() => _enabled = false;
    }
}
