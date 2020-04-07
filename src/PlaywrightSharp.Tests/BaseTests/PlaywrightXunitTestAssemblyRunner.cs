using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <inheritdoc/>
    public class PlaywrightXunitTestAssemblyRunner : XunitTestAssemblyRunner
    {
        /// <inheritdoc/>
        public PlaywrightXunitTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions) : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        /// <inheritdoc cref="XunitTestAssemblyRunner.AfterTestAssemblyStartingAsync"/>
        protected override async Task AfterTestAssemblyStartingAsync()
        {
            await base.AfterTestAssemblyStartingAsync();
            await PlaywrightSharpLoader.SetupAsync();
        }

        /// <inheritdoc cref="XunitTestAssemblyRunner.BeforeTestAssemblyFinishedAsync"/>
        protected override async Task BeforeTestAssemblyFinishedAsync()
        {
            await base.BeforeTestAssemblyFinishedAsync();
            await PlaywrightSharpLoader.TeardownAsync();
        }
    }
}
