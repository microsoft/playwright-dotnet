using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.NUnitTest
{
    public class PlaywrightTest
    {
        public static string BrowserName => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROWSER")) ?
            "chromium" : Environment.GetEnvironmentVariable("BROWSER").ToLower();

        private static Task<IPlaywright> _playwrightTask = Microsoft.Playwright.Playwright.CreateAsync();

        public IPlaywright Playwright { get; private set; }
        public IBrowserType BrowserType { get; private set; }

        [SetUp]
        public async Task PlaywrightSetup()
        {
            Playwright = await _playwrightTask;
            BrowserType = Playwright[BrowserName];
        }

        public static async Task<T> AssertThrowsAsync<T>(Func<Task> action) where T : System.Exception
        {
            try
            {
                await action();
                Assert.Fail();
                return null;
            }
            catch (T t)
            {
                return t;
            }
        }

        public static void DebugLog(string text)
        {
            TestContext.Progress.WriteLine(text);
        }

        public bool TestOk()
        {
            return
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed ||
                TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Skipped;
        }
    }
}
