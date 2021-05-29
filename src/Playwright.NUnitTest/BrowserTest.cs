using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.NUnitTest
{
    public class BrowserTest : PlaywrightTest
    {
        private static ConcurrentStack<IBrowser> browserPool_ = new ConcurrentStack<IBrowser>();

        public IBrowser Browser { get; private set; }

        [SetUp]
        public async Task BrowserSetup()
        {
            IBrowser browser;
            if (browserPool_.TryPop(out browser))
            {
                Browser = browser;
                return;
            }

            Browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        [TearDown]
        public async Task BrowserTeardown()
        {
            if (TestOk())
            {
                browserPool_.Push(Browser);
            }
            else
            {
                await Browser.CloseAsync();
            }
        }
    }
}
