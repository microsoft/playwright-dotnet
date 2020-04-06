using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("PlaywrightSharp.Tests.BaseTests.PlaywrightAssemblyFixture", "PlaywrightSharp.Tests")]
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <inheritdoc/>
    public class PlaywrightAssemblyFixture : XunitTestAssemblyRunner
    {
        /// <inheritdoc/>
        public PlaywrightAssemblyFixture(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions) : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
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
