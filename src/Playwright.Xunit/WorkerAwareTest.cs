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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.TestAdapter;
using Xunit;

namespace Microsoft.Playwright.Xunit;

public class WorkerAwareTest : ExceptionCapturer
{
    private static readonly ConcurrentStack<Worker> _allWorkers = new();
    private Worker _currentWorker = null!;

    internal class Worker
    {
        private static int _lastWorkedIndex = 0;
        public int WorkerIndex = Interlocked.Increment(ref _lastWorkedIndex);
        public Dictionary<string, IWorkerService> Services = [];
    }

    public int WorkerIndex { get; internal set; }

    public async Task<T> RegisterService<T>(string name, Func<Task<T>> factory) where T : class, IWorkerService
    {
        if (!_currentWorker.Services.ContainsKey(name))
        {
            _currentWorker.Services[name] = await factory().ConfigureAwait(false);
        }

        return (_currentWorker.Services[name] as T)!;
    }

    async public override Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);
        if (!_allWorkers.TryPop(out _currentWorker!))
        {
            _currentWorker = new();
        }
        WorkerIndex = _currentWorker.WorkerIndex;
        if (PlaywrightSettingsProvider.ExpectTimeout.HasValue)
        {
            AssertionsBase.SetDefaultTimeout(PlaywrightSettingsProvider.ExpectTimeout.Value);
        }
    }

    public async override Task DisposeAsync()
    {
        if (TestOk)
        {
            foreach (var kv in _currentWorker.Services)
            {
                await kv.Value.ResetAsync().ConfigureAwait(false);
            }
            _allWorkers.Push(_currentWorker);
        }
        else
        {
            foreach (var kv in _currentWorker.Services)
            {
                await kv.Value.DisposeAsync().ConfigureAwait(false);
            }
            _currentWorker.Services.Clear();
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }
}

public interface IWorkerService
{
    public Task ResetAsync();
    public Task DisposeAsync();
}

/// <summary>
/// ExceptionCapturer is a best-effort way of detecting if a test did pass or fail in xUnit.
/// This class uses the AppDomain's FirstChanceException event to set a flag indicating
/// whether an exception has occurred during the test execution.
/// 
/// Note: There is no way of getting the test status in xUnit in the dispose method.
/// For more information, see: https://stackoverflow.com/questions/28895448/current-test-status-in-xunit-net
/// </summary>
public class ExceptionCapturer : IAsyncLifetime
{
    protected bool TestOk { get; private set; } = true;

    public ExceptionCapturer()
    {
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            TestOk = false;
        };
    }

    public virtual Task InitializeAsync()
    {
        TestOk = true;
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
