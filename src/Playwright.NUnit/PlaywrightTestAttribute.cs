/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Playwright.TestAdapter;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnitFrameworkBase = NUnit.Framework;

[assembly: InternalsVisibleToAttribute("Microsoft.Playwright.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleToAttribute("Playwright.TestingHarnessTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]

namespace Microsoft.Playwright.NUnit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal class PlaywrightTestAttribute : NUnitFrameworkBase.TestAttribute, IWrapTestMethod
{
    public PlaywrightTestAttribute() : base()
    {
    }

    public TestCommand Wrap(TestCommand command) => new RetryTestCommand(command);

    internal class RetryTestCommand : BeforeTestCommand
    {

        public RetryTestCommand(TestCommand innerCommand) : base(innerCommand) { }

        public override TestResult Execute(TestExecutionContext context)
        {
            List<SlimTestResult> failedResults = new List<SlimTestResult>();
            bool tearDownRan = false;
            context.CurrentTest.Properties.Set("RetryCount", 0);
            while (true)
            {
                // NUnit takes care about running SetUp for the first iteration. If we run Execute more than once, we need to manually run SetUp.
                if (Test2RetryCount(context.CurrentTest) > 0)
                {
                    Task.WaitAll(RunSetUpOrTearDownMethod(context, isSetUp: true));
                }
                try
                {
                    (context.CurrentResult, tearDownRan) = ExecuteWithTimeout(context);
                }
                catch (Exception ex)
                {
                    context.CurrentResult = context.CurrentTest.MakeTestResult();
                    context.CurrentResult.RecordException(ex);
                }
                if (context.CurrentResult.ResultState == ResultState.Success)
                {
                    break;
                }
                failedResults.Add(new SlimTestResult(context.CurrentResult));
                if (Test2RetryCount(context.CurrentTest) == PlaywrightSettingsProvider.Retries)
                {
                    break;
                }
                // NUnit takes care about running TearDown for the last iteration. PlaywrightTestAttribute inherits from
                // the TimeoutCommand which runs TearDown for us in a case of a timeout.
                if (!tearDownRan)
                {
                    Task.WaitAll(RunSetUpOrTearDownMethod(context, isTearDown: true));
                }
                context.CurrentTest.Properties.Set("RetryCount", Test2RetryCount(context.CurrentTest) + 1);
            }
            if (Test2RetryCount(context.CurrentTest) > 0)
            {
                if (!string.IsNullOrEmpty(context.CurrentResult.Output))
                {
                    context.CurrentResult.OutWriter.Write("\n");
                }
                var retryCount = Test2RetryCount(context.CurrentTest);
                context.CurrentResult.OutWriter.Write(GenerateOutputLog(failedResults, retryCount));
            }
            return context.CurrentResult;
        }

        private static int Test2RetryCount(Test test)
            => (int)test.Properties.Get("RetryCount")!;

        private static string GenerateOutputLog(List<SlimTestResult> failedResults, int retryCount)
        {
            var logSeparator = new String('=', 80);
            string output = $"{logSeparator}\n";
            output += $"Test was retried {retryCount} time{(retryCount > 1 ? "s" : "")}.\n";

            if (failedResults.Count > 0)
            {
                output += $"\nFailing test runs:\n";
                foreach (var (result, retry) in failedResults.Select((run, i) => (run, i)))
                {
                    if (result == null)
                    {
                        continue;
                    }
                    output += new String('-', 40) + "\n";
                    output += $"  Test: {result.FullName} (retry #{retry})\n";
                    output += $"  Outcome: {result.ResultState}\n";
                    if (!string.IsNullOrEmpty(result.Output))
                    {
                        output += $"  Standard Output Messages: \n{Indent(result.Output.TrimEnd(), 4)}\n";
                    }
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        output += $"  Message \n{Indent(result.Message!.TrimEnd(), 4)}\n";
                    }
                }
            }

            output += logSeparator;
            return output;
        }

        private static string Indent(string text, int indent)
        {
            return text.Split(new[] { "\n" }, StringSplitOptions.None).Select(line => new String(' ', indent) + line).Aggregate((a, b) => a + "\n" + b);
        }

        // In case of a execution timeout, we run the teardown handler manually.
        // To prevent running teardown twice, we need to keep track of it.
        private (TestResult, bool) ExecuteWithTimeout(TestExecutionContext context)
        {
            var timeout = PlaywrightSettingsProvider.TestTimeout;
            if (timeout == null || Debugger.IsAttached)
            {
                return (innerCommand.Execute(context), false);
            }

            if (CheckThatPlaywrightTimeoutIsSmallerThanNUnitTimeout(context, timeout.Value) is TestResult result)
            {
                return (result, false);
            };

            return ExecuteWithTimeoutInnerAsync(context, timeout.Value).GetAwaiter().GetResult();
        }

        private async Task<(TestResult, bool)> ExecuteWithTimeoutInnerAsync(TestExecutionContext context, int timeout)
        {
            bool tearDownRan = false;
            string timeoutExceptionMessage = $"Test exceeded Timeout value of {timeout}ms";
            // 1. Race Test execution against the specified timeout.
            var testExecutionTask = Task.Run(() => innerCommand.Execute(context));
            var timeoutTask = Task.Delay(timeout);
            await Task.WhenAny(testExecutionTask, timeoutTask);

            // 2. If timeout was reached, call the tearDownHandlers and
            // cancel by that the underlying BrowserContext calls.
            if (timeoutTask.IsCompleted)
            {
                // 2.1 Call the test teardown handlers.
                await RunSetUpOrTearDownMethod(context, isTearDown: true);
                tearDownRan = true;

                // 2.2 Wait for the test execution to complete (let e.g. Page.ClickAsync throw)
                // and give the test 5 seconds to finish.
                await Task.WhenAny(testExecutionTask, Task.Delay(5000));
            }
            TestResult result;
            if (!testExecutionTask.IsCompleted)
            {
                result = context.CurrentTest.MakeTestResult();
                result.RecordException(new TimeoutException(timeoutExceptionMessage));
                return (result, tearDownRan);
            }
            try
            {
                return (await testExecutionTask, tearDownRan);
            }
            catch (System.Exception exception)
            {
                result = context.CurrentTest.MakeTestResult();
                result.RecordException(new AggregateException(new Exception[] { new TimeoutException(timeoutExceptionMessage), exception }), FailureSite.Test);
                return (result, tearDownRan);
            }
        }

        protected async Task RunSetUpOrTearDownMethod(TestExecutionContext context, bool isSetUp = false, bool isTearDown = false)
        {
            if (isSetUp == isTearDown)
            {
                throw new ArgumentException("isSetUp and isTearDown cannot be both true or both false");
            }
            // https://github.com/nunit/nunit/blob/f77ac94d69daf985653763f073e651304d9e31f8/src/NUnitFramework/framework/Internal/Execution/SimpleWorkItem.cs#L94
            // Try to locate the parent fixture. In current implementations, the test method
            // is either one or two levels below the TestFixture - if this changes,
            // so should the following code.
            TestFixture? parentFixture = Test.Parent as TestFixture ?? Test.Parent?.Parent as TestFixture;

            // In normal operation we should always get the methods from the parent fixture.
            // However, some of NUnit's own tests can create a TestMethod without a parent
            // fixture. Most likely, we should stop doing this, but it affects 100s of cases.
            IMethodInfo[] methods;
            if (isSetUp)
            {
                methods = parentFixture?.SetUpMethods ?? Test.TypeInfo!.GetMethodsWithAttribute<SetUpAttribute>(true);
            }
            else
            {
                methods = parentFixture?.TearDownMethods ?? Test.TypeInfo!.GetMethodsWithAttribute<TearDownAttribute>(true);
            }

            foreach (var method in methods)
            {
                // https://github.com/nunit/nunit/blob/f77ac94d69daf985653763f073e651304d9e31f8/src/NUnitFramework/framework/Internal/Commands/SetUpTearDownItem.cs#L103
                var result = method.Invoke(method.IsStatic ? null : context.TestObject, null);
                if (result is Task task)
                {
                    await task;
                }
            }
        }

        private TestResult? CheckThatPlaywrightTimeoutIsSmallerThanNUnitTimeout(TestExecutionContext context, int playwrightTestTimeout)
        {
            // See https://github.com/nunit/nunit/blob/f77ac94d69daf985653763f073e651304d9e31f8/src/NUnitFramework/framework/Internal/Execution/SimpleWorkItem.cs#L142-L148
            // If a timeout is specified, create a TimeoutCommand
            // Timeout set at a higher level
            int timeout = context.TestCaseTimeout;
            // Timeout set on this test
            if (Test.Properties.ContainsKey(PropertyNames.Timeout))
                timeout = (int)Test.Properties.Get(PropertyNames.Timeout)!;

            if (timeout != 0 && playwrightTestTimeout > timeout)
            {
                var result = context.CurrentTest.MakeTestResult();
                result.RecordException(new InvalidOperationException($"The Playwright test timeout of {playwrightTestTimeout}ms is higher than the NUnit test timeout of {timeout}ms. Set the Playwright test timeout to a lower value."));
                return result;
            }
            return null;
        }
    }

    private class SlimTestResult
    {
        public string FullName { get; }
        public string Output { get; }
        public ResultState ResultState { get; }
        public string? Message { get; }

        internal SlimTestResult(TestResult result)
        {
            FullName = result.FullName;
            Output = result.Output;
            ResultState = result.ResultState;
            Message = result.Message;
        }
    }
}
