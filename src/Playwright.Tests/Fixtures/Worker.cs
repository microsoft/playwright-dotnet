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

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The worker fixtures that one test uses at a time: the driver, the browser, the test servers. It stands
/// for a worker process of Playwright Test. A worker goes back to the pool after a test that passed, and
/// is closed after a test that failed, as Playwright Test restarts the worker process.
/// </summary>
internal sealed class Worker
{
    private static int _lastWorkerIndex = -1;
    private HashSet<IBrowserContext> _contextsBeforeTest = [];

    public Worker(string signature, int parallelIndex)
    {
        Signature = signature;
        Info = new WorkerInfo(Interlocked.Increment(ref _lastWorkerIndex), parallelIndex);
    }

    public string Signature { get; }

    public WorkerInfo Info { get; }

    public FixtureStore Store { get; } = new();

    public HashSet<Type> BeforeAllDone { get; } = [];

    public List<Func<Task>> AfterAll { get; } = [];

    private IBrowser Browser => Store.Find("browser")?.Value as IBrowser;

    public void WillRunTest()
    {
        _contextsBeforeTest = Browser is { IsConnected: true } browser ? [.. browser.Contexts] : [];
    }

    /// <summary>Closes the contexts that the test left open in the browser of the worker.</summary>
    public async Task CloseLeftoverContextsAsync(List<Exception> errors)
    {
        if (Browser is not { IsConnected: true } browser)
        {
            return;
        }
        foreach (var context in browser.Contexts)
        {
            if (_contextsBeforeTest.Contains(context))
            {
                continue;
            }
            try
            {
                await context.CloseAsync(new() { Reason = "Test ended." }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }
        }
    }

    public async Task DisposeAsync()
    {
        var errors = new List<Exception>();
        foreach (var hook in AfterAll)
        {
            try
            {
                await hook().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }
        }
        AfterAll.Clear();
        await Store.TeardownAsync(errors).ConfigureAwait(false);
        foreach (var error in errors)
        {
            await Console.Error.WriteLineAsync($"Error while closing worker {Info.WorkerIndex}: {error}").ConfigureAwait(false);
        }
    }
}

internal static class WorkerPool
{
    private const int DisposeTimeout = 30_000;
    private static readonly object _lock = new();
    private static readonly List<Worker> _idle = [];
    private static readonly HashSet<int> _usedParallelIndexes = [];

    static WorkerPool()
    {
        // Playwright Test removes the output directory at the start of a run.
        try
        {
            if (Directory.Exists(Env.OutputDir))
            {
                Directory.Delete(Env.OutputDir, true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        // The workers stay in the pool until the end of the run. This closes the browsers and the drivers
        // when no [OneTimeTearDown] did.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
#pragma warning disable VSTHRD002
                DisposeAllAsync().Wait(TimeSpan.FromSeconds(5));
#pragma warning restore VSTHRD002
            }
            catch
            {
                // Best effort, the process is exiting.
            }
        };
    }

    public static async Task<Worker> AcquireAsync(string signature)
    {
        Worker stale = null;
        lock (_lock)
        {
            var index = _idle.FindLastIndex(worker => worker.Signature == signature);
            if (index != -1)
            {
                var worker = _idle[index];
                _idle.RemoveAt(index);
                return worker;
            }
            // A worker with other worker fixtures makes room for this one, as Playwright Test
            // stops a worker process to start one with other worker fixtures.
            if (_idle.Count > 0)
            {
                stale = _idle[0];
                _idle.RemoveAt(0);
            }
        }
        if (stale != null)
        {
            await DisposeAsync(stale).ConfigureAwait(false);
        }
        lock (_lock)
        {
            var parallelIndex = 0;
            while (!_usedParallelIndexes.Add(parallelIndex))
            {
                parallelIndex++;
            }
            return new Worker(signature, parallelIndex);
        }
    }

    public static void Release(Worker worker)
    {
        lock (_lock)
        {
            _idle.Add(worker);
        }
    }

    public static async Task DisposeAsync(Worker worker)
    {
        try
        {
            var dispose = worker.DisposeAsync();
            if (await Task.WhenAny(dispose, Task.Delay(DisposeTimeout)).ConfigureAwait(false) != dispose)
            {
                await Console.Error.WriteLineAsync($"Worker {worker.Info.WorkerIndex} did not close in {DisposeTimeout}ms").ConfigureAwait(false);
            }
        }
        finally
        {
            lock (_lock)
            {
                _usedParallelIndexes.Remove(worker.Info.ParallelIndex);
            }
        }
    }

    public static async Task DisposeAllAsync()
    {
        List<Worker> workers;
        lock (_lock)
        {
            workers = [.. _idle];
            _idle.Clear();
        }
        await Task.WhenAll(workers.Select(DisposeAsync)).ConfigureAwait(false);
    }
}
