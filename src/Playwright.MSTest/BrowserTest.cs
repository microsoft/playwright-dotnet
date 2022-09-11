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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.MSTest.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.MSTest;

[TestClass]
public class BrowserTest : PlaywrightTest
{
    public IBrowser Browser { get; private set; } = null!;
    private readonly List<IBrowserContext> _contexts = new();

    public async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options)
    {
        var context = await Browser!.NewContextAsync(options).ConfigureAwait(false);
        _contexts.Add(context);
        return context;
    }

    [TestInitialize]
    public async Task BrowserSetup()
    {
        Browser = (await GetService<BrowserService>().ConfigureAwait(false)).Browser;
    }

    [TestCleanup]
    public async Task BrowserTearDown()
    {
        if (TestOK)
        {
            foreach (var context in _contexts)
            {
                await context.CloseAsync().ConfigureAwait(false);
            }
        }
        _contexts.Clear();
        Browser = null!;
    }
}
