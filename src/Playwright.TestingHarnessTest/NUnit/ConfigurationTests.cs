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

using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Playwright.TestingHarnessTest.NUnit
{
    [SetUpFixture]
    public class AssemblyConfiguration
    {
        internal static string _userAgentName = "SpecialUserAgent-v111";

        [OneTimeSetUp]
        public void ConfigureGlobal()
        {
            PlaywrightConfiguration.Global.BrowserNewContextOptions.UserAgent = _userAgentName;
            PlaywrightConfiguration.Global.BrowserNewContextOptions.ColorScheme = Microsoft.Playwright.ColorScheme.Dark;
        }
    }

    public class GlobalConfigurationTest : PageTest
    {
        [Test]
        public async Task ShouldUseGloballyConfiguredUserAgent()
        {
            await Page.SetContentAsync("<html></html>");
            Assert.AreEqual(AssemblyConfiguration._userAgentName, await Page.EvaluateAsync<string>("() => navigator.userAgent"));
        }
    }

    public class ConfigurationOverrideTest : PageTest
    {
        private static readonly string _userAgent = "NewUserAgent-v1";

        [OneTimeSetUp]
        public void DoSomething()
        {
            Configuration.BrowserNewContextOptions.UserAgent = _userAgent;
        }

        [Test]
        public async Task ShouldOverrideConfig()
        {
            await Page.SetContentAsync("<html></html>");
            Assert.AreEqual(_userAgent, await Page.EvaluateAsync<string>("() => navigator.userAgent"));
        }

        [Test]
        public async Task EnsureGlobalConfigIsStillFollowed()
        {
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }
    }
}
