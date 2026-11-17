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

using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>testInfo.project</c>.</summary>
public sealed class TestProject
{
    /// <summary><c>chromium-page</c>, <c>firefox-library</c>: the browser of the run.</summary>
    public string Name => Env.BrowserName;

    /// <summary><c>testInfo.project.outputDir</c>.</summary>
    public string OutputDir => Env.OutputDir;
}

/// <summary><c>testInfo</c> upstream: the <c>testInfo</c> parameter of a test, a hook or a fixture, and <c>test.info()</c>. It also has the calls that upstream makes on <c>test</c> inside a test or a hook: <c>test.skip(condition)</c> is <c>Info.Skip(condition)</c>.</summary>
public sealed class TestInfo
{
    internal TestInfo(string title, string id, string file, string testName, int retry, int workerIndex, int parallelIndex)
    {
        Title = title;
        Id = id;
        File = file;
        Retry = retry;
        WorkerIndex = workerIndex;
        ParallelIndex = parallelIndex;
        Timeout = Env.Timeout;
        var directoryName = Regex.Replace($"{(file ?? "test").Replace(".spec.ts", string.Empty)}-{id}-{testName}", @"[^A-Za-z0-9_.-]+", "-");
        if (directoryName.Length > 100)
        {
            directoryName = directoryName.Substring(0, 100);
        }
        OutputDir = Path.Combine(Env.OutputDir, $"{directoryName}-{Env.BrowserName}{(retry > 0 ? $"-retry{retry}" : string.Empty)}");
        TestId = Regex.Replace($"{id}-{testName}-{Env.BrowserName}", @"[^A-Za-z0-9_-]+", "-");
        if (Directory.Exists(OutputDir))
        {
            Directory.Delete(OutputDir, true);
        }
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

    /// <summary>'passed', 'failed' or 'skipped', for a teardown. A test that timed out has no teardown.</summary>
    public string Status => TestContext.CurrentContext.Result.Outcome.Status switch
    {
        TestStatus.Passed => "passed",
        TestStatus.Skipped => "skipped",
        _ => "failed",
    };

    /// <summary>The directory for the files of this test. It is empty when the test starts.</summary>
    public string OutputDir { get; }

    /// <summary><c>test.skip(condition, description)</c>.</summary>
    public void Skip(bool condition, string description = null)
    {
        if (condition)
        {
            Assert.Ignore(description ?? "skip");
        }
    }

    /// <summary>
    /// <c>test.fixme(condition, description)</c>: a skip that upstream has. Also
    /// <c>test.fail(condition, description)</c>, which the port does not run.
    /// </summary>
    public void Fixme(bool condition, string description = null)
    {
        if (condition)
        {
            Assert.Ignore(description == null ? "fixme" : "fixme: " + description);
        }
    }

    /// <summary><c>test.slow()</c>: three times the timeout.</summary>
    public void Slow() => Timeout *= 3;

    /// <summary><c>test.slow(condition, description)</c>.</summary>
    public void Slow(bool condition, string description = null)
    {
        if (condition)
        {
            Slow();
        }
    }

    /// <summary><c>test.setTimeout(timeout)</c>.</summary>
    public void SetTimeout(int timeout) => Timeout = timeout;

    /// <summary><c>await test.step(title, async () => { ... })</c>.</summary>
    public Task Step(string title, Func<Task> body) => body();

    /// <summary><c>const value = await test.step(title, async () => { ... })</c>.</summary>
    public Task<T> Step<T>(string title, Func<Task<T>> body) => body();

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

}
