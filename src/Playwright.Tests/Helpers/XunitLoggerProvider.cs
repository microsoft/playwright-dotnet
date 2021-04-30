using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests.Helpers
{
    internal sealed class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _helper;
        private readonly List<XunitLogger> _loggers = new List<XunitLogger>();

        public void Dispose()
        {
            foreach (var logger in _loggers)
            {
                logger.Dispose();
            }
            _loggers.Clear();
        }

        public XunitLoggerProvider(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new XunitLogger(_helper);
            _loggers.Add(logger);
            return logger;
        }
    }
}
