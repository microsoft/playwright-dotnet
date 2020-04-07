using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("PlaywrightSharp.Tests.BaseTests.PlaywrightXunitTestFramework", "PlaywrightSharp.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PlaywrightSharp.Tests.BaseTests
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
