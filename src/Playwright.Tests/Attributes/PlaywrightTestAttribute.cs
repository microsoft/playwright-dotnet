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

namespace Microsoft.Playwright.Tests;

/// <summary>
/// Enables decorating test facts with information about the corresponding test in the upstream repository.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class PlaywrightTestAttribute : TestAttribute, IWrapSetUpTearDown
{
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
    /// Creates a new instance of the attribute.
    /// </summary>
    /// <param name="fileName"><see cref="FileName"/></param>
    /// <param name="describe"><see cref="Describe"/></param>
    /// <param name="nameOfTest"><see cref="TestName"/></param>
    public PlaywrightTestAttribute(string fileName, string describe, string nameOfTest) : this(fileName, nameOfTest)
    {
        Describe = describe;
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

    /// <summary>
    /// Wraps the current test command in a <see cref="UnobservedTaskExceptionCommand"/>.
    /// </summary>
    /// <param name="command">the test command</param>
    /// <returns>the wrapped test command</returns>
    public TestCommand Wrap(TestCommand command)
        => new UnobservedTaskExceptionCommand(command);

    /// <summary>
    /// Helper to detect UnobservedTaskExceptions
    /// </summary>
    private sealed class UnobservedTaskExceptionCommand : NUnit.PlaywrightTestAttribute.RetryTestCommand
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
                result = base.Execute(context);
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
