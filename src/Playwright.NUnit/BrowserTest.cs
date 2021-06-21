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

namespace Microsoft.Playwright.NUnit
{
    public class BrowserTest : PlaywrightTest
    {
        public IBrowser Browser { get; internal set; }
        private readonly List<IBrowserContext> _contexts = new();

        public async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
        {
            var context = await Browser.NewContextAsync(options);
            _contexts.Add(context);
            return context;
        }

        [SetUp]
        public async Task BrowserSetup()
        {
            var service = await BrowserService.Register(this, BrowserType);
            Browser = service.Browser;
        }

        [TearDown]
        public async Task BrowserTearDown()
        {
            if (TestOk())
            {
                foreach (var context in _contexts)
                {
                    await context.CloseAsync();
                }
            }
            _contexts.Clear();
            Browser = null;
        }
    }
}
