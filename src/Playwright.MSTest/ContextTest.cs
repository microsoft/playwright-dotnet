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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.MSTest;

[TestClass]
public class ContextTest : BrowserTest
{
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _failedTraces = new();

    public IBrowserContext Context { get; private set; } = null!;
    private bool _isTracing;

    [TestInitialize]
    public async Task ContextSetup()
    {
        Context = await NewContextAsync(ContextOptions()).ConfigureAwait(false);
        if (TracingOptions() is { } tracingOptions)
        {
            if (TestRunCount() == 1)
            {
                _failedTraces.TryRemove(TraceKey(), out _);
            }
            await Context.Tracing.StartAsync(tracingOptions).ConfigureAwait(false);
            _isTracing = true;
        }
    }

    [TestCleanup]
    public async Task ContextTearDown()
    {
        if (!_isTracing)
        {
            return;
        }

        _isTracing = false;
        if (!TestFailed())
        {
            await Context.Tracing.StopAsync().ConfigureAwait(false);
            AttachFailedTraces();
            return;
        }

        var resultsDirectory = TestContext.ResultsDirectory ?? Path.GetTempPath();
        Directory.CreateDirectory(resultsDirectory);
        var tracePath = Path.Combine(resultsDirectory, TraceFileName());
        await Context.Tracing.StopAsync(new() { Path = tracePath }).ConfigureAwait(false);
        var traces = _failedTraces.GetOrAdd(TraceKey(), _ => new());
        traces.Add(tracePath);
        foreach (var path in traces)
        {
            TestContext.AddResultFile(path);
        }
    }

    public virtual BrowserNewContextOptions ContextOptions()
    {
        return new()
        {
            Locale = "en-US",
            ColorScheme = ColorScheme.Light,
        };
    }

    /// <summary>
    /// Options used to record a trace that is retained and attached only when a test fails.
    /// Return <see langword="null"/> to disable tracing.
    /// </summary>
    public virtual TracingStartOptions? TracingOptions() => null;

    private string TraceFileName()
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        var testName = new string(TestDisplayName().Select(c => invalidFileNameChars.Contains(c) ? '_' : c).ToArray());
        return $"{testName}-run-{TestRunCount()}-{Guid.NewGuid():N}.zip";
    }

    private void AttachFailedTraces()
    {
        if (_failedTraces.TryRemove(TraceKey(), out var traces))
        {
            foreach (var path in traces)
            {
                TestContext.AddResultFile(path);
            }
        }
    }

    private string TraceKey() => $"{TestContext.FullyQualifiedTestClassName}.{TestDisplayName()}";

    private string TestDisplayName()
    {
        var property = TestContext.GetType().GetProperty("TestDisplayName");
        return property?.GetValue(TestContext) as string ?? TestContext.TestName;
    }

    private bool TestFailed()
    {
        var property = TestContext.GetType().GetProperty("TestException");
        return property?.GetValue(TestContext) is Exception || !TestOK();
    }

    private int TestRunCount()
    {
        var property = TestContext.GetType().GetProperty("TestRunCount");
        return property?.GetValue(TestContext) is int testRunCount ? testRunCount : 1;
    }
}
