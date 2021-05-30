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
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.NUnitTest
{
    public class WorkerAwareTest
    {
        internal class Worker
        {
            private static int lastWorkerIndex_ = 0;
            public int WorkerIndex = Interlocked.Increment(ref lastWorkerIndex_);
            public Dictionary<string, IWorkerService> Services = new Dictionary<string, IWorkerService>();
        }

        private static ConcurrentStack<Worker> allWorkers_ = new ConcurrentStack<Worker>();
        private Worker currentWorker_;

        public int WorkerIndex { get; internal set; }

        public async Task<T> RegisterService<T>(string name, Func<Task<T>> factory) where T : class, IWorkerService
        {
            if (!currentWorker_.Services.ContainsKey(name))
            {
                currentWorker_.Services[name] = await factory();
            }

            return currentWorker_.Services[name] as T;
        }

        [SetUp]
        public void WorkerSetup()
        {
            if (!allWorkers_.TryPop(out currentWorker_))
            {
                currentWorker_ = new Worker();
            }
            WorkerIndex = currentWorker_.WorkerIndex;
        }

        [TearDown]
        public async Task WorkerTeardown()
        {
            if (TestOk())
            {
                foreach (var kv in currentWorker_.Services)
                {
                    await kv.Value.ResetAsync();
                }
                allWorkers_.Push(currentWorker_);
            }
            else
            {
                foreach (var kv in currentWorker_.Services)
                {
                    await kv.Value.DisposeAsync();
                }
                currentWorker_.Services.Clear();
            }
        }

        public bool TestOk()
        {
            return
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed ||
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Skipped;
        }
    }
}
