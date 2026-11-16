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

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>An entry of <c>testInfo.annotations</c>. The port keeps them, nothing reads them.</summary>
public sealed class TestAnnotation
{
    public string Type { get; set; }

    public string Description { get; set; }
}

/// <summary><c>testInfo.project</c>.</summary>
public sealed class TestProject
{
    /// <summary><c>chromium-page</c>, <c>firefox-library</c>: the browser of the run.</summary>
    public string Name => Env.BrowserName;

    /// <summary><c>testInfo.project.outputDir</c>.</summary>
    public string OutputDir => Env.OutputDir;
}

/// <summary><c>testInfo</c> upstream: the parameter <c>TestInfo testInfo</c> of a test, a hook or a fixture, and <c>It.Info()</c>.</summary>
public sealed class TestInfo
{
    private readonly Stopwatch _clock = new();
    private int _pauseDepth;

    internal TestInfo(string title, string id, string file, string testName, int retry, WorkerInfo worker)
    {
        Title = title;
        Id = id;
        File = file;
        Retry = retry;
        WorkerIndex = worker.WorkerIndex;
        ParallelIndex = worker.ParallelIndex;
        Timeout = Env.Timeout;
        var directoryName = Regex.Replace($"{(file ?? "test").Replace(".spec.ts", string.Empty)}-{id}-{testName}", @"[^A-Za-z0-9_.-]+", "-");
        if (directoryName.Length > 100)
        {
            directoryName = directoryName.Substring(0, 100);
        }
        OutputDir = Path.Combine(Env.OutputDir, $"{directoryName}-{Env.BrowserName}{(retry > 0 ? $"-retry{retry}" : string.Empty)}");
        TestId = Regex.Replace($"{id}-{testName}-{Env.BrowserName}", @"[^A-Za-z0-9_-]+", "-");
    }

    /// <summary>The upstream title, with the identifier.</summary>
    public string Title { get; }

    /// <summary>The registry identifier, <c>CH-001</c>.</summary>
    public string Id { get; }

    /// <summary><c>testInfo.testId</c>: unique for the test in the run.</summary>
    public string TestId { get; }

    /// <summary><c>testInfo.project</c>.</summary>
    public TestProject Project { get; } = new();

    /// <summary>The upstream file, relative to <c>tests/</c>: <c>page/page-check.spec.ts</c>. Null when the registry does not have the test.</summary>
    public string File { get; }

    public int Retry { get; }

    public int WorkerIndex { get; }

    public int ParallelIndex { get; }

    /// <summary>The timeout of the test in milliseconds, 0 for none.</summary>
    public int Timeout { get; private set; }

    public List<TestAnnotation> Annotations { get; } = [];

    /// <summary>'passed', 'failed', 'timedOut' or 'skipped'. Set when the test body has finished, for the teardown of the fixtures.</summary>
    public string Status { get; internal set; }

    /// <summary>'passed', or 'failed' after <c>It.Fail()</c>.</summary>
    public string ExpectedStatus { get; internal set; } = "passed";

    /// <summary>The directory for the files of this test. It is empty when the test starts.</summary>
    public string OutputDir { get; }

    internal string ExpectedFailureReason { get; set; }

    internal bool Started => _clock.IsRunning || _clock.ElapsedTicks > 0;

    public void SetTimeout(int timeout) => Timeout = timeout;

    public void Slow() => Timeout *= 3;

    /// <summary><c>testInfo.attach(name, { body, contentType })</c>: the port has no report to attach to.</summary>
    public Task Attach(string name, object options = null) => Task.CompletedTask;

    /// <summary><c>testInfo.outputPath('a', 'b.txt')</c>.</summary>
    public string OutputPath(params string[] pathSegments)
    {
        Directory.CreateDirectory(OutputDir);
        return Path.GetFullPath(Path.Combine([OutputDir, .. pathSegments]));
    }

    /// <summary>
    /// <c>testInfo.snapshotPath('x.png')</c>: the file in the <c>-snapshots</c> directory of the upstream
    /// test file, <c>{testFileName}-snapshots/{arg}-{browserName}{ext}</c>.
    /// </summary>
    public string SnapshotPath(params string[] pathSegments)
    {
        if (File == null)
        {
            throw new InvalidOperationException($"{Id} is not in the upstream registry, so it has no snapshots");
        }
        var name = Path.Combine(pathSegments);
        var extension = Path.GetExtension(name);
        var withBrowser = name.Substring(0, name.Length - extension.Length) + "-" + Env.BrowserName + extension;
        return Path.Combine(Env.UpstreamTestsDir, File.Replace('/', Path.DirectorySeparatorChar) + "-snapshots", withBrowser);
    }

    internal void Start()
    {
        if (Directory.Exists(OutputDir))
        {
            Directory.Delete(OutputDir, true);
        }
        _clock.Start();
    }

    /// <summary>The milliseconds left, or null when there is no timeout.</summary>
    internal double? Remaining()
    {
        if (Timeout <= 0 || Debugger.IsAttached)
        {
            return null;
        }
        return Timeout - _clock.Elapsed.TotalMilliseconds;
    }

    internal void RestartTimeout() => _clock.Restart();

    internal void PauseTimeout()
    {
        if (Interlocked.Increment(ref _pauseDepth) == 1)
        {
            _clock.Stop();
        }
    }

    internal void ResumeTimeout()
    {
        if (Interlocked.Decrement(ref _pauseDepth) == 0)
        {
            _clock.Start();
        }
    }
}
