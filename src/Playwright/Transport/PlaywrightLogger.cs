using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Playwright.Transport
{
    internal class PlaywrightLogger : ILogger
    {
        private static readonly Dictionary<string, ConsoleColor> _debugLoggerColorMap = new()
        {
            { "pw:api", ConsoleColor.Cyan },
            { "pw:protocol", ConsoleColor.Green },
            { "pw:install", ConsoleColor.Green },
            { "pw:proxy", ConsoleColor.DarkCyan },
            { "pw:error", ConsoleColor.Red },
            { "pw:channel:command", ConsoleColor.Blue },
            { "pw:channel:response", ConsoleColor.DarkYellow },
            { "pw:channel:event", ConsoleColor.Magenta },
        };

        private static readonly Regex _messageTypeRegex = new(@"(pw:[^\s]+)", RegexOptions.IgnoreCase);

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var messageTypeMatch = _messageTypeRegex.Match(message);
            if (messageTypeMatch?.Success ?? false)
            {
                var group = messageTypeMatch.Groups[0];
                if (_debugLoggerColorMap.TryGetValue(group.Value, out ConsoleColor consoleColor))
                {
                    // Note: I'm using this instead of \x1b[91m ... \x1b[0m because Test Explorer's output window refuses to render
                    //  the message if it contains the above escape sequence.
                    Console.ResetColor();
                    Console.Error.Write(message.Substring(0, messageTypeMatch.Index));
                    Console.ForegroundColor = consoleColor;
                    Console.Error.Write(message.Substring(messageTypeMatch.Index, messageTypeMatch.Length));
                    Console.ResetColor();
                    message = message.Substring(messageTypeMatch.Index + messageTypeMatch.Length);
                }
            }

            Console.ResetColor();
            Console.Error.WriteLine(message);
        }

        internal class PlaywrightLoggerProvider : ILoggerProvider
        {
            private readonly ConcurrentDictionary<string, PlaywrightLogger> _loggers = new();

            public ILogger CreateLogger(string categoryName) =>
                _loggers.GetOrAdd(categoryName, _ => new PlaywrightLogger());

            public void Dispose() => _loggers.Clear();
        }
    }
}
