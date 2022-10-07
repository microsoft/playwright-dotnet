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
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.NUnit;

public class WorkerAwareTest
{
    internal class Worker
    {
        private static int _lastWorkedIndex = 0;
        public int WorkerIndex = Interlocked.Increment(ref _lastWorkedIndex);
        public Dictionary<string, IWorkerService> Services = new();
    }

    private static readonly ConcurrentStack<Worker> _allWorkers = new();
    private Worker _currentWorker = null!;

    public int WorkerIndex { get; internal set; }

    public async Task<T> RegisterService<T>(string name, Func<Task<T>> factory) where T : class, IWorkerService
    {
        if (!_currentWorker.Services.ContainsKey(name))
        {
            _currentWorker.Services[name] = await factory().ConfigureAwait(false);
        }

        return (_currentWorker.Services[name] as T)!;
    }

    [SetUp]
    public void WorkerSetup()
    {
        if (!_allWorkers.TryPop(out _currentWorker))
        {
            _currentWorker = new();
        }
        WorkerIndex = _currentWorker.WorkerIndex;
        if (PlaywrightSettingsProvider.ExpectTimeout.HasValue)
        {
            AssertionsBase.SetDefaultTimeout(PlaywrightSettingsProvider.ExpectTimeout.Value);
        }
    }

    [TearDown]
    public async Task WorkerTeardown()
    {
        if (TestOk())
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
    }

    public bool TestOk()
    {
        return
            TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed ||
            TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Skipped;
    }
}
