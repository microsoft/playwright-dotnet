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

using System.Diagnostics;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// Marks the port of one upstream test, <c>it('CH-001 should check the box @smoke', async ({ page }) => ...)</c>,
/// in a class that derives from <see cref="UpstreamPlaywrightTest"/>. The title is the upstream title, with
/// the identifier and the tags. See <c>src/Playwright.Tests/AGENTS.md</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class UpstreamTestAttribute : TestAttribute, IApplyToTest, IRepeatTest
{
    private static readonly Regex _idRegex = new(@"^([A-Z]{2}-(?:[A-Z]{2,3}-)?\d{3})(?:\s|$)", RegexOptions.Compiled);

    public UpstreamTestAttribute(string title)
    {
        Title = title;
        Description = title;
    }

    /// <summary>The upstream title, as written in the <c>it()</c> call.</summary>
    public string Title { get; }

    /// <summary>The registry identifier, the first token of the title.</summary>
    public string Id => _idRegex.Match(Title) is { Success: true } match ? match.Groups[1].Value : null;

    public new void ApplyToTest(Test test)
    {
        base.ApplyToTest(test);
        if (Id == null)
        {
            test.MakeInvalid($"The title \"{Title}\" does not start with a registry identifier, for example \"CH-001 \"");
            return;
        }
        // The tests of a class run in the order of the source, which is the upstream order.
        test.Properties.Set(PropertyNames.Order, test.Method.MethodInfo.MetadataToken & 0xFFFFFF);
        // --filter "TestCategory=CH-001", "TestCategory=@smoke".
        test.Properties.Add(PropertyNames.Category, Id);
        foreach (Match tag in Regex.Matches(Title, @"(?<=\s)@[\w-]+"))
        {
            test.Properties.Add(PropertyNames.Category, tag.Value);
        }
    }

    /// <summary>
    /// NUnit puts the command of <see cref="IRepeatTest"/> around the one that makes the instance of the
    /// test class, so every try of a test has an instance of its own.
    /// </summary>
    public TestCommand Wrap(TestCommand command)
    {
        // It.Slow() and It.SetTimeout() change the timeout while the test runs.
        command = new PlaywrightTestAttribute.TimeoutCommand(command, () =>
        {
            var timeout = (TestExecutionContext.CurrentContext.TestObject as UpstreamPlaywrightTest)?.TestInfo?.Timeout ?? Env.Timeout;
            return timeout <= 0 || Debugger.IsAttached ? int.MaxValue : timeout;
        });
        if (Env.CI)
        {
            command = new PlaywrightTestAttribute.RetryCommand(command, 3);
        }
        return new AbandonedTasksCommand(command);
    }

    /// <summary>
    /// The old tests fail on the exception of a task that nobody awaited, when the task is collected.
    /// A task that this test abandoned, for example a call that was pending when the test timed out,
    /// is collected here, so that it does not fail an old test later.
    /// </summary>
    private sealed class AbandonedTasksCommand(TestCommand innerCommand) : DelegatingTestCommand(innerCommand)
    {
        public override TestResult Execute(TestExecutionContext context)
        {
            EventHandler<UnobservedTaskExceptionEventArgs> observe = (_, e) => e.SetObserved();
            TaskScheduler.UnobservedTaskException += observe;
            try
            {
                return innerCommand.Execute(context);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                TaskScheduler.UnobservedTaskException -= observe;
            }
        }
    }
}
