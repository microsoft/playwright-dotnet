using System;
namespace Microsoft.Playwright.Tests.Helpers
{
    internal static class BrowserExtensions
    {
        public static int GetMajorVersion(this IBrowser browser) => Convert.ToInt32(browser.Version.Split('.')[0]);
    }
}
