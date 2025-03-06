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
public class PlaywrightTestAttribute : TestAttribute, IApplyToContext, IApplyToTest, IWrapSetUpTearDown
{
    private readonly CancelAfterAttribute _cancelAfterAttribute = new(TestConstants.DefaultTestTimeout);

    public PlaywrightTestAttribute()
    {
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
    /// Returns the trimmed file name.
    /// </summary>
    public string TrimmedName => FileName.Substring(0, FileName.IndexOf('.'));

    /// <summary>
    /// The name of the test, the decorated code is based on.
    /// </summary>
    public string TestName { get; }

    /// <summary>
    /// The describe of the test, the decorated code is based on, if one exists.
    /// </summary>
    public string Describe { get; }

    public void ApplyToContext(TestExecutionContext context)
    {
        if (context.TestCaseTimeout == 0)
        {
            (_cancelAfterAttribute as IApplyToContext).ApplyToContext(context);
        }
    }

    public new void ApplyToTest(Test test)
    {
        base.ApplyToTest(test);
        if (TestExecutionContext.CurrentContext.TestCaseTimeout == 0)
        {
            _cancelAfterAttribute.ApplyToTest(test);
        }
    }
    /// <summary>
    /// Wraps the current test command in a <see cref="UnobservedTaskExceptionCommand"/>.
    /// </summary>
    /// <param name="command">the test command</param>
    /// <returns>the wrapped test command</returns>
    public TestCommand Wrap(TestCommand command)
    {
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
}
