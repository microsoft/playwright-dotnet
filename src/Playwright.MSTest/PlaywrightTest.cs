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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.TestAdapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.MSTest;

[TestClass]
public class PlaywrightTest
{
    private static int _workerCount = 0;
    private static readonly ConcurrentStack<Worker> _allWorkers = new();
    private Worker? _currentWorker;
    private static readonly Task<IPlaywright> _playwrightTask = Microsoft.Playwright.Playwright.CreateAsync();

    public string BrowserName { get; private set; } = null!;
    public IPlaywright Playwright { get; private set; } = null!;

    public IBrowserType BrowserType { get; private set; } = null!;

    public int WorkerIndex { get => _currentWorker!.WorkerIndex; }

    [TestInitialize]
    public async Task Setup()
    {
        if (PlaywrightSettingsProvider.ExpectTimeout.HasValue)
        {
            AssertionsBase.SetDefaultTimeout(PlaywrightSettingsProvider.ExpectTimeout.Value);
        }
        try
        {
            Playwright = await _playwrightTask.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message, e.StackTrace);
        }
        Assert.IsNotNull(Playwright, "Playwright could not be instantiated.");
        BrowserName = PlaywrightSettingsProvider.BrowserName;
        BrowserType = Playwright[BrowserName];

        // get worker
        if (!_allWorkers.TryPop(out _currentWorker))
        {
            _currentWorker = new();
        }
    }

    [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
    public static void ClassInitialize(TestContext context)
    {
        PlaywrightTestMethodAttribute.TestContext = context;
    }

    [TestCleanup]
    public async Task Teardown()
    {
        if (TestOK)
        {
            await Task.WhenAll(_currentWorker!.InstantiatedServices.Select(x => x.ResetAsync())).ConfigureAwait(false);
            _allWorkers.Push(_currentWorker);
        }
        else
        {
            await Task.WhenAll(_currentWorker!.InstantiatedServices.Select(x => x.DisposeAsync())).ConfigureAwait(false);
            _currentWorker.InstantiatedServices.Clear();
        }
    }

    public async Task<T> GetService<T>(Func<T>? factory = null) where T : class, IWorkerService, new()
    {
        factory ??= () => new T();
        var serviceType = typeof(T);

        var instance = _currentWorker!.InstantiatedServices.SingleOrDefault(x => serviceType.IsInstanceOfType(x));
        if (instance == null)
        {
            instance = factory();
            await instance.BuildAsync(this).ConfigureAwait(false);
            _currentWorker.InstantiatedServices.Add(instance);
        }

        if (instance is not T)
            throw new Exception("There was a problem instantiating the service.");

        return (T)instance;
    }

    private class Worker
    {
        public int WorkerIndex { get; } = Interlocked.Increment(ref _workerCount);
        public List<IWorkerService> InstantiatedServices { get; } = new();
    }

    protected bool TestOK
    {
        get => TestContext!.CurrentTestOutcome == UnitTestOutcome.Passed
            || TestContext!.CurrentTestOutcome == UnitTestOutcome.NotRunnable;
    }

    public TestContext TestContext { get; set; } = null!;

    public ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    public IPageAssertions Expect(IPage page) => Assertions.Expect(page);

    public IAPIResponseAssertions Expect(IAPIResponse response) => Assertions.Expect(response);
}
