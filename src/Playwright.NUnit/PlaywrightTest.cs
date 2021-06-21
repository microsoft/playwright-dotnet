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
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.NUnit
{
    public class PlaywrightTest : WorkerAwareTest
    {
        public static string BrowserName => (Environment.GetEnvironmentVariable("BROWSER") ?? Microsoft.Playwright.BrowserType.Chromium).ToLower();

        private static readonly Task<IPlaywright> _playwrightTask = Microsoft.Playwright.Playwright.CreateAsync();

        public IPlaywright Playwright { get; private set; }
        public IBrowserType BrowserType { get; private set; }

        [SetUp]
        public async Task PlaywrightSetup()
        {
            Playwright = await _playwrightTask;
            BrowserType = Playwright[BrowserName];
            Assert.IsNotNull(BrowserType, $"The requested browser ({BrowserName}) could not be found - make sure your BROWSER env variable is set correctly.");
        }
    }
}
