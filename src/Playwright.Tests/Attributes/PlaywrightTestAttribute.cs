/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

// Run all tests in sequence
[assembly: LevelOfParallelism(1)]
[assembly: Parallelizable(ParallelScope.Fixtures)]

namespace Microsoft.Playwright.Tests;

/// <summary>
/// Enables decorating test facts with information about the corresponding test in the upstream repository.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class PlaywrightTestAttribute : TestAttribute, IWrapSetUpTearDown
{
    private readonly int? _timeout;

    public PlaywrightTestAttribute()
    {
    }

    public PlaywrightTestAttribute(int timeout) : this()
    {
        _timeout = timeout;
    }

    public PlaywrightTestAttribute(string fileName, string nameOfTest, int timeout) : this(fileName, nameOfTest)
    {
        _timeout = timeout;
    }

    /// <summary>
    /// Creates a new instance of the attribute.
    /// </summary>
    /// <param name="fileName"><see cref="FileName"/></param>
    /// <param name="nameOfTest"><see cref="TestName"/></param>
    public PlaywrightTestAttribute(string fileName, string nameOfTest)
    {
        FileName = fileName;
        TestName = nameOfTest;
    }

    /// <summary>
    /// The file name origin of the test.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The name of the test, the decorated code is based on.
    /// </summary>
    public string TestName { get; }

    /// <summary>
    /// The describe of the test, the decorated code is based on, if one exists.
    /// </summary>
    public string Describe { get; }

    /// <summary>
    /// Wraps the current test command in a <see cref="UnobservedTaskExceptionCommand"/>.
    /// </summary>
    /// <param name="command">the test command</param>
    /// <returns>the wrapped test command</returns>
    public TestCommand Wrap(TestCommand command)
    {
        command = new TimeoutCommand(command, _timeout ?? TestConstants.DefaultTestTimeout);
        if (Environment.GetEnvironmentVariable("CI") != null)
        {
            command = new RetryCommand(command, 3);
        }
        return new UnobservedTaskExceptionCommand(command);
    }

    // RetryAttribute.RetryCommand only retries AssertionException but we want to retry all exceptions. See
    // https://github.com/nunit/nunit/issues/1388#issuecomment-2574970271
    internal class RetryCommand(TestCommand innerCommand, int retryCount) : DelegatingTestCommand(innerCommand)
    {
        private readonly int _retryCount = retryCount;

        public override TestResult Execute(TestExecutionContext context)
        {
            int tryCount = 0;
            bool isPassed = false;

            while (tryCount < _retryCount)
            {
                try
                {
                    innerCommand.Execute(context);
                    if (context.CurrentResult.ResultState == ResultState.Success)
                    {
                        isPassed = true;
                        break;
                    }
                }
                catch (Exception)
                {
                    // Ignore the exception
                }

                tryCount++;
                if (tryCount < _retryCount)
                {
                    // Reset only if there will be another retry
                    context.CurrentResult = context.CurrentTest.MakeTestResult();
                }

            }

            LogFinalOutcome(context.CurrentResult, tryCount == 0, isPassed);

            return context.CurrentResult;
        }

        private void LogFinalOutcome(TestResult result, bool firstAttempt, bool isPassed)
        {
            if (!firstAttempt)
            {
                Console.Error.WriteLine($"WARNING: Test {result.FullName} needed {_retryCount} retries and {(isPassed ? "passed" : "failed")}");
            }
        }
    }

    /// <summary>
    /// Helper to detect UnobservedTaskExceptions
    /// </summary>
    private sealed class UnobservedTaskExceptionCommand : DelegatingTestCommand
    {
        public UnobservedTaskExceptionCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
        }

        private readonly List<Exception> _unobservedTaskExceptions = new();

        public override TestResult Execute(TestExecutionContext context)
        {
            TaskScheduler.UnobservedTaskException += UnobservedTaskException;
            TestResult result = null;
            try
            {
                result = innerCommand.Execute(context);
            }
            finally
            {
                // force a GC and wait for finalizers of (among other things) Tasks
                // for which the UnobservedTaskException is raised if the task.Exception was not observed 
                GC.Collect();
                GC.WaitForPendingFinalizers();

                TaskScheduler.UnobservedTaskException -= UnobservedTaskException;

                if (_unobservedTaskExceptions.Count > 0)
                {
                    result.RecordTearDownException(new AggregateException(_unobservedTaskExceptions));
                    foreach (var exc in _unobservedTaskExceptions)
                    {
                        Console.WriteLine("UnobservedTaskExceptions:");
                        Console.WriteLine(exc);
                        Console.WriteLine(exc.Message);
                        Console.WriteLine(exc.StackTrace);
                    }
                    _unobservedTaskExceptions.Clear();
                }
            }
            return result;
        }

        private void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _unobservedTaskExceptions.Add(e.Exception);
        }
    }

    /// <summary>
    /// <see cref="TimeoutCommand"/> creates a timer in order to cancel
    /// a test if it exceeds a specified time and adjusts
    /// the test result if it did time out.
    /// </summary>
    public class TimeoutCommand : BeforeAndAfterTestCommand
    {
        private readonly int _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutCommand"/> class.
        /// </summary>
        /// <param name="innerCommand">The inner command</param>
        /// <param name="timeout">Timeout value</param>
        /// <param name="debugger">An <see cref="IDebugger" /> instance</param>
        internal TimeoutCommand(TestCommand innerCommand, int timeout) : base(innerCommand)
        {
            _timeout = timeout;
        }

        /// <summary>
        /// Runs the test, saving a TestResult in the supplied TestExecutionContext.
        /// </summary>
        /// <param name="context">The context in which the test should run.</param>
        /// <returns>A TestResult</returns>
        public override TestResult Execute(TestExecutionContext context)
        {
            try
            {
                using (new TestExecutionContext.IsolatedContext())
                {
                    var testExecution = Task.Run(() => innerCommand.Execute(TestExecutionContext.CurrentContext));
                    var timedOut = Task.WaitAny([testExecution], _timeout) == -1;

                    if (timedOut)
                    {
                        context.CurrentResult.SetResult(
                            ResultState.Failure,
                            $"Test exceeded Timeout value of {_timeout}ms");
                        // // When the timeout is reached the TearDown methods are not called. This is a best-effort
                        // // attempt to call them and close the browser / http server.
                        // foreach (var method in new string[] { "WorkerTeardown", "BrowserTearDown" })
                        // {
                        //     var methodFun = context.CurrentTest.Method.MethodInfo.DeclaringType
                        //         .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        //     if (methodFun != null)
                        //     {
                        //         var res = methodFun.Invoke(context.TestObject, null);
                        //         if (res is Task task)
                        //         {
                        //             Console.WriteLine($"Waiting for {method} task to complete");
                        //             task.GetAwaiter().GetResult();
                        //         }
                        //     }
                        // }
                    }
                    else
                    {
                        context.CurrentResult = testExecution.GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception exception)
            {
                context.CurrentResult.RecordException(exception, FailureSite.Test);
            }

            return context.CurrentResult;
        }
    }
}
