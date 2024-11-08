using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.TestAdapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Microsoft.Playwright.MSTest;

public class WorkerAwareTest
{
    public TestContext TestContext { get; set; } = null!;

    private static readonly ConcurrentStack<Worker> _allWorkers = new();
    private Worker _currentWorker = null!;

    private class Worker
    {
        private static int _lastWorkedIndex = 0;
        public int WorkerIndex { get; } = Interlocked.Increment(ref _lastWorkedIndex);
        public Dictionary<string, IWorkerService> Services = [];
    }

    public int WorkerIndex => _currentWorker!.WorkerIndex;

    public async Task<T> RegisterService<T>(string name, Func<Task<T>> factory) where T : class, IWorkerService
    {
        if (!_currentWorker.Services.ContainsKey(name))
        {
            _currentWorker.Services[name] = await factory().ConfigureAwait(false);
        }

        return (_currentWorker.Services[name] as T)!;
    }

    [TestInitialize]
    public void WorkerSetup()
    {
        if (PlaywrightSettingsProvider.ExpectTimeout.HasValue)
        {
            AssertionsBase.SetDefaultTimeout(PlaywrightSettingsProvider.ExpectTimeout.Value);
        }

        if (!_allWorkers.TryPop(out _currentWorker))
        {
            _currentWorker = new();
        }
    }

    [TestCleanup]
    public async Task WorkerTeardown()
    {
        if (TestOK())
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

    protected bool TestOK()
    {
        return TestContext!.CurrentTestOutcome == UnitTestOutcome.Passed
            || TestContext!.CurrentTestOutcome == UnitTestOutcome.NotRunnable;
    }
}

public interface IWorkerService
{
    public Task ResetAsync();
    public Task DisposeAsync();
}
