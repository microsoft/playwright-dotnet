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
using System.Globalization;
using System.Linq;
using System.Reflection;
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

        Console.WriteLine("Playwright timeout: {0}", timeout.Value);
        return base.Execute(new TimeoutTestMethod(testMethod, timeout.Value));
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

    internal class TimeoutTestMethod : ITestMethod
    {
        private readonly ITestMethod _testMethod;
        private readonly int _timeout;
        private readonly TimeoutMethodInfo _timeoutMethodInfo;

        // Proxy from the passed ITestMethod:
        public string TestMethodName => _testMethod.TestMethodName;
        public string TestClassName => _testMethod.TestClassName;
        public Type ReturnType => _testMethod.ReturnType;
        public object[] Arguments => _testMethod.Arguments;
        public ParameterInfo[] ParameterTypes => _testMethod.ParameterTypes;
        public Attribute[] GetAllAttributes(bool inherit) => _testMethod.GetAllAttributes(inherit);
        public AttributeType[] GetAttributes<AttributeType>(bool inherit) where AttributeType : Attribute => _testMethod.GetAttributes<AttributeType>(inherit);
        public TestResult Invoke(object[] arguments) => _testMethod.Invoke(arguments);

        // Custom overriden MethodInfo via TimeoutMethodInfo class:
        public MethodInfo MethodInfo => _timeoutMethodInfo;

        public TimeoutTestMethod(ITestMethod testMethod, int timeout)
        {
            _testMethod = testMethod;
            _timeout = timeout;
            _timeoutMethodInfo = new TimeoutMethodInfo(testMethod.MethodInfo, timeout);
        }
    }

    class TimeoutMethodInfo : MethodInfo
    {
        private readonly MethodInfo _parent;
        private readonly int _timeout;

        // Proxy from the passed MethodInfo:
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => _parent.ReturnTypeCustomAttributes;
        public override MethodAttributes Attributes => _parent.Attributes;
        public override RuntimeMethodHandle MethodHandle => _parent.MethodHandle;
        public override Type DeclaringType => _parent.DeclaringType;
        public override string Name => _parent.Name;
        public override Type ReflectedType => _parent.ReflectedType;
        public override MethodInfo GetBaseDefinition() => _parent.GetBaseDefinition();
        public override object[] GetCustomAttributes(bool inherit) => _parent.GetCustomAttributes(inherit);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _parent.GetCustomAttributes(attributeType, inherit);
        public override MethodImplAttributes GetMethodImplementationFlags() => _parent.GetMethodImplementationFlags();
        public override ParameterInfo[] GetParameters() => _parent.GetParameters();
        public override bool IsDefined(Type attributeType, bool inherit) => _parent.IsDefined(attributeType, inherit);

        // Custom overriden Invoke and race against the timeout:
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            Console.WriteLine("heyhoPlaywright timeout: {0}", _timeout);
            var result = _parent.Invoke(obj, invokeAttr, binder, parameters, culture);
            if (!(result is Task))
                return result;
            Console.WriteLine($"Waiting for test to finish within {_timeout}ms");
            using(System.IO.StreamWriter sw = System.IO.File.AppendText("/Users/maxschmitt/Developer/playwright-dotnet/log.txt"))
            {
                sw.WriteLine("GeeksforGeek1s");
            }
            return InvokeAsync((Task<object>)result).GetAwaiter().GetResult();
        }

        private async Task<object> InvokeAsync(Task<object> testExecutionTask)
        {
            using(System.IO.StreamWriter sw = System.IO.File.AppendText("/Users/maxschmitt/Developer/playwright-dotnet/log.txt"))
            {
                sw.WriteLine("GeeksforGeeks");
            }
            var timeoutTask = Task.Delay(_timeout);
            await Task.WhenAny(testExecutionTask, timeoutTask);

            // 2. Close the BrowserContext
            if (ContextCloseHookOnTimeoutAsync != null)
            {
                await ContextCloseHookOnTimeoutAsync().ConfigureAwait(false);
            }
            // 3. If timeout was reached, we need to wait for the test execution to finish, since Page.Click could hang.
            if (!timeoutTask.IsCompleted)
            {
                throw new TimeoutException($"Test timed out after {_timeout}ms.");
            }
            // 3.1 Wait for the test execution to complete (let e.g. Page.ClickAsync throw)
            // and give the test 5 seconds to finish.
            return await testExecutionTask;
        }

        public TimeoutMethodInfo(MethodInfo parent, int timeout)
        {
            this._parent = parent;
            this._timeout = timeout;
        }
    }
}
