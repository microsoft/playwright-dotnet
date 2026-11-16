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

using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework.Internal;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// Runs one ported test as Playwright Test runs the upstream one: takes a worker, runs the hooks and the
/// test with their fixtures under the timeout of the test, tears the test fixtures down, and gives the
/// worker back or closes it after a failure.
/// </summary>
internal static class TestRunner
{
    private const float DefaultExpectTimeout = 5_000;
    private static readonly object _logLock = new();

    public static async Task RunAsync(object testObject, MethodInfo method, object[] caseArguments)
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var fullName = TestContext.CurrentContext.Test.FullName;
        var classInfo = TestClassInfo.For(testObject?.GetType() ?? method.DeclaringType);
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(Env.ExpectTimeout);
        Log($"start {fullName}");
        var outcome = "passed";
        try
        {
            for (var retry = 0; ; retry++)
            {
                Exception error;
                // NUnit fails a test for a recorded assertion failure, also when the test passed on a
                // retry or was expected to fail.
                using (new TestExecutionContext.IsolatedContext())
                {
                    error = await RunAttemptAsync(classInfo, testObject, method, caseArguments, testName, fullName, retry).ConfigureAwait(false);
                }
                if (error == null)
                {
                    if (retry > 0)
                    {
                        await Console.Error.WriteLineAsync($"WARNING: flaky: {classInfo.Type.FullName}.{testName} passed on retry #{retry}").ConfigureAwait(false);
                    }
                    return;
                }
                if (IsSkip(error) || retry >= Env.Retries)
                {
                    ExceptionDispatchInfo.Capture(error).Throw();
                }
                await Console.Error.WriteLineAsync($"WARNING: {classInfo.Type.FullName}.{testName} failed, retry #{retry + 1}: {error.Message}").ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            outcome = IsSkip(e) ? "skipped" : "failed";
            throw;
        }
        finally
        {
            Log($"end   {fullName} {outcome}");
            Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(DefaultExpectTimeout);
            ObserveAbandonedTasks();
        }
    }

    /// <summary>
    /// <c>PWTEST_LOG=file</c> appends a line when a test starts and when it ends. The test runner prints
    /// the results of a long run late, so this is the way to see which test a run hangs in.
    /// </summary>
    private static void Log(string line)
    {
        if (Env.LogFile == null)
        {
            return;
        }
        lock (_logLock)
        {
            File.AppendAllText(Env.LogFile, $"{DateTime.Now:HH:mm:ss.fff} {line}\n");
        }
    }

    /// <summary>
    /// The old tests fail on the exception of a task that nobody awaited, when the task is collected.
    /// A task that this test abandoned, for example a call that was pending when the test timed out,
    /// is collected here, so that it does not fail an old test later.
    /// </summary>
    private static void ObserveAbandonedTasks()
    {
        EventHandler<UnobservedTaskExceptionEventArgs> observe = (_, e) => e.SetObserved();
        TaskScheduler.UnobservedTaskException += observe;
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= observe;
        }
    }

    private static bool IsSkip(Exception error) => error is IgnoreException or InconclusiveException;

    private static async Task<Exception> RunAttemptAsync(TestClassInfo classInfo, object testObject, MethodInfo method, object[] caseArguments, string testName, string fullName, int retry)
    {
        var attribute = UpstreamTestAttribute.Of(method);
        var marks = new List<MemberInfo> { method };
        marks.AddRange(classInfo.Chain.Reverse());
        if (marks.Select(mark => mark.GetCustomAttribute<ItSkipAttribute>(false)).FirstOrDefault(mark => mark != null) is { } skip)
        {
            return new IgnoreException(skip.Reason ?? "skip");
        }
        if (marks.Select(mark => mark.GetCustomAttribute<ItFixmeAttribute>(false)).FirstOrDefault(mark => mark != null) is { } declaredFixme)
        {
            return new IgnoreException(declaredFixme.Reason == null ? "fixme" : "fixme: " + declaredFixme.Reason);
        }

        var worker = await WorkerPool.AcquireAsync(classInfo.WorkerSignature).ConfigureAwait(false);
        var testInfo = new TestInfo(attribute.Title, attribute.Id, UpstreamRegistry.Find(attribute.Id)?.File, testName, retry, worker.Info);
        if (marks.Any(mark => mark.GetCustomAttribute<ItSlowAttribute>(false) != null))
        {
            testInfo.Slow();
        }
        if (marks.Select(mark => mark.GetCustomAttribute<ItFailAttribute>(false)).FirstOrDefault(mark => mark != null) is { } fail)
        {
            testInfo.ExpectedStatus = "failed";
            testInfo.ExpectedFailureReason = fail.Reason == null ? "fail" : "fail: " + fail.Reason;
        }
        var fixme = method.GetCustomAttributes<FixmeAttribute>(false).FirstOrDefault(mark => Applies(mark, fullName));

        var scope = new TestScope(worker, classInfo, testObject, testInfo);
        TestScope.Current = scope;
        worker.WillRunTest();
        testInfo.Start();

        Exception error = null;
        var timedOut = false;
        var skippedInBeforeEach = false;
        var body = RunBodyAsync(scope, method, caseArguments, () => skippedInBeforeEach = true);
        if (await WaitAsync(body, testInfo).ConfigureAwait(false))
        {
            try
            {
                await body.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                error = e;
            }
        }
        else
        {
            timedOut = true;
            error = new TimeoutException($"Test timeout of {testInfo.Timeout}ms exceeded.");
            // The test is still running. The teardown below makes its calls fail.
            _ = body.ContinueWith(task => _ = task.Exception, TaskScheduler.Default);
        }
        testInfo.Status = timedOut ? "timedOut" : error == null ? "passed" : IsSkip(error) ? "skipped" : "failed";

        var teardownErrors = new List<Exception>();
        testInfo.RestartTimeout();
        var teardown = TeardownAsync(scope, runAfterEach: !skippedInBeforeEach, teardownErrors);
        if (!await WaitAsync(teardown, testInfo).ConfigureAwait(false))
        {
            timedOut = true;
            error ??= new TimeoutException($"Tearing down the fixtures exceeded the test timeout of {testInfo.Timeout}ms.");
            _ = teardown.ContinueWith(task => _ = task.Exception, TaskScheduler.Default);
        }
        else if (error == null || IsSkip(error))
        {
            error = teardownErrors.FirstOrDefault() ?? error;
        }
        TestScope.Current = null;

        if (error == null || (IsSkip(error) && !timedOut))
        {
            WorkerPool.Release(worker);
        }
        else
        {
            await WorkerPool.DisposeAsync(worker).ConfigureAwait(false);
        }

        if (error != null && IsSkip(error))
        {
            return error;
        }
        if (fixme != null)
        {
            if (error == null)
            {
                return fixme.Strict ? new AssertionException($"The test passes. Remove [Fixme(\"{fixme.Reason}\")].") : null;
            }
            return new IgnoreException($"fixme: {fixme.Reason}\n{error.GetType().Name}: {error.Message}");
        }
        if (testInfo.ExpectedStatus == "failed")
        {
            return error != null ? null : new AssertionException($"The test is expected to fail ({testInfo.ExpectedFailureReason}), and it passed.");
        }
        return error;
    }

    private static bool Applies(FixmeAttribute fixme, string testName)
    {
        if (fixme.Browsers != null && !Split(fixme.Browsers).Contains(Env.BrowserName))
        {
            return false;
        }
        if (fixme.Platforms != null && !Split(fixme.Platforms).Contains(Env.Platform))
        {
            return false;
        }
        return fixme.Case == null || testName.Contains(fixme.Case, StringComparison.Ordinal);
    }

    private static string[] Split(string list) => list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static async Task RunBodyAsync(TestScope scope, MethodInfo method, object[] caseArguments, Action skippedInBeforeEach)
    {
        // The body starts on another thread, so that a test that blocks does not block the timeout.
        await Task.Yield();
        try
        {
            await scope.RunBeforeAllAsync().ConfigureAwait(false);
            await scope.SetupAutoFixturesAsync().ConfigureAwait(false);
            foreach (var hook in scope.ClassInfo.BeforeEach)
            {
                await scope.InvokeAsync(hook).ConfigureAwait(false);
            }
        }
        catch (Exception e) when (IsSkip(e))
        {
            skippedInBeforeEach();
            throw;
        }
        await scope.InvokeAsync(method, caseArguments).ConfigureAwait(false);
    }

    private static async Task TeardownAsync(TestScope scope, bool runAfterEach, List<Exception> errors)
    {
        await Task.Yield();
        if (runAfterEach)
        {
            foreach (var hook in scope.ClassInfo.AfterEach)
            {
                try
                {
                    await scope.InvokeAsync(hook).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }
        }
        await scope.TeardownAsync(errors).ConfigureAwait(false);
        await scope.Worker.CloseLeftoverContextsAsync(errors).ConfigureAwait(false);
    }

    /// <summary>Returns false when the timeout of the test, which the test can change, ends before the task.</summary>
    private static async Task<bool> WaitAsync(Task task, TestInfo testInfo)
    {
        while (!task.IsCompleted)
        {
            var remaining = testInfo.Remaining();
            if (remaining <= 0)
            {
                return false;
            }
            await Task.WhenAny(task, Task.Delay((int)Math.Min(remaining ?? 200, 200))).ConfigureAwait(false);
        }
        return true;
    }
}
