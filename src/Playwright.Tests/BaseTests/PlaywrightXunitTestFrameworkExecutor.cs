using System.Collections.Generic;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Playwright.Tests.BaseTests
{
    /// <inheritdoc/>
    public class PlaywrightXunitTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        /// <inheritdoc/>
        public PlaywrightXunitTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink) : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        /// <inheritdoc/>
        protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using (var assemblyRunner = new PlaywrightXunitTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
            {
                assemblyRunner.RunAsync();
            }
        }
    }
}
