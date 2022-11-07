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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.TestAdapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.MSTest;

internal class PlaywrightTimeoutAttribute : TestMethodAttribute
{
    static internal Func<Task>? ContextCloseHookOnTimeoutAsync { get; set; } = null;

    public override TestResult[] Execute(ITestMethod testMethod)
    {
        var timeout = PlaywrightSettingsProvider.TestTimeout;
        if (timeout == null || Debugger.IsAttached)
        {
            return base.Execute(testMethod);
        }
        if (EnsureThatPlaywrightTimeoutIsSmallerThanNUnitTimeout(testMethod, timeout.Value) is TestResult[] result)
        {
            return result;
        }

        return ExecuteWithTimeoutAsync(testMethod, timeout.Value).GetAwaiter().GetResult();
    }

    private async Task<TestResult[]> ExecuteWithTimeoutAsync(ITestMethod testMethod, int timeout)
    {
        // 1. Race Test execution against the specified timeout.
        // In MSTest base.Execute will internally run the TestInitialize and TestCleanup. In case of a timeout, we don't
        // run TestCleanup, only TestInitialize. To get good error messages, we close the browser context inside ContextCloseHookOnTimeoutAsync.
        var testExecutionTask = Task.Run(() => base.Execute(testMethod));
        var timeoutTask = Task.Delay(timeout);
        await Task.WhenAny(testExecutionTask, timeoutTask);

        // 2. Close the BrowserContext
        if (ContextCloseHookOnTimeoutAsync != null)
        {
            await ContextCloseHookOnTimeoutAsync().ConfigureAwait(false);
        }

        // 3. If timeout was reached, we need to wait for the test execution to finish, since Page.Click could hang.
        if (timeoutTask.IsCompleted)
        {
            // 3.1 Wait for the test execution to complete (let e.g. Page.ClickAsync throw)
            // and give the test 5 seconds to finish.
            await Task.WhenAny(testExecutionTask, Task.Delay(5000));
        }

        // 4. If the test execution is still running, we know it was a timeout.
        if (!testExecutionTask.IsCompleted)
        {
            return new TestResult[]
            {
                new TestResult()
                {
                    Outcome = UnitTestOutcome.Timeout,
                    TestFailureException = new Exception($"Test timed out after {timeout}ms.")
                },
            };
        }

        return await testExecutionTask;
    }

    private TestResult[]? EnsureThatPlaywrightTimeoutIsSmallerThanNUnitTimeout(ITestMethod testMethod, int playwrightTestTimeout)
    {
        var timeout = MSTestProvider.TestTimeout;
        var timeoutAttribute = testMethod.MethodInfo.GetCustomAttributes(typeof(TimeoutAttribute), false).FirstOrDefault();
        if (timeoutAttribute != null)
        {
            timeout = ((TimeoutAttribute)timeoutAttribute).Timeout;
        }

        if (timeout != null && timeout != 0 && playwrightTestTimeout > timeout)
        {
            return new TestResult[]
            {
                new TestResult()
                {
                    Outcome = UnitTestOutcome.Failed,
                    TestFailureException = new InvalidOperationException($"Playwright test timeout ({playwrightTestTimeout}ms) is larger than MSTest timeout ({timeout}ms). Set the Playwright test timeout to a lower value or disable the MSTest timeout.")
                },
            };
        }
        return null;
    }
}
