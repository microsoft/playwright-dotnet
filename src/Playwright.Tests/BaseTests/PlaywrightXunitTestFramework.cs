using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Microsoft.Playwright.Tests.BaseTests.PlaywrightXunitTestFramework", "Microsoft.Playwright.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.Playwright.Tests.BaseTests
{
    /// <inheritdoc/>
    public class PlaywrightXunitTestFramework : XunitTestFramework
    {
        /// <inheritdoc/>
        public PlaywrightXunitTestFramework(IMessageSink messageSink) : base(messageSink)
        {
        }

        /// <inheritdoc/>
        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
            => new PlaywrightXunitTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }
}
