/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    /// <playwright-file>defaultbrowsercontext-2.spec.ts</playwright-file>
    public class DefaultBrowsercontext2Tests : PlaywrightTestEx
    {
        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support hasTouch option")]
        public async Task ShouldSupportHasTouchOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                HasTouch = true
            });

            await page.GotoAsync(Server.Prefix + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should work in persistent context")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldWorkInPersistentContext()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                ViewportSize = new()
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });

            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(980, await page.EvaluateAsync<int>("() => window.innerWidth"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support colorScheme option")]
        public async Task ShouldSupportColorSchemeOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                ColorScheme = ColorScheme.Dark,
            });

            Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support reducedMotion option")]
        public async Task ShouldSupportReducedMotionOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                ReducedMotion = ReducedMotion.Reduce
            });

            Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"));
            Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support timezoneId option")]
        public async Task ShouldSupportTimezoneIdOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                TimezoneId = "America/Jamaica",
            });

            Assert.AreEqual("Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)", await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support locale option")]
        public async Task ShouldSupportLocaleOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                Locale = "fr-CH",
            });

            Assert.AreEqual("fr-CH", await page.EvaluateAsync<string>("() => navigator.language"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support geolocation and permissions options")]
        public async Task ShouldSupportGeolocationAndPermissionsOptions()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                Geolocation = new()
                {
                    Latitude = 10,
                    Longitude = 10,
                },
                Permissions = new[] { "geolocation" },
            });

            await page.GotoAsync(Server.EmptyPage);
            var geolocation = await page.EvaluateAsync<Geolocation>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
            Assert.AreEqual(10, geolocation.Latitude);
            Assert.AreEqual(10, geolocation.Longitude);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support ignoreHTTPSErrors option")]
        public async Task ShouldSupportIgnoreHTTPSErrorsOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                IgnoreHTTPSErrors = true
            });

            var response = await page.GotoAsync(HttpsServer.Prefix + "/empty.html");
            Assert.True(response.Ok);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support extraHTTPHeaders option")]
        public async Task ShouldSupportExtraHTTPHeadersOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
            });

            string fooHeader = string.Empty;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", r => fooHeader = r.Headers["foo"]),
                page.GotoAsync(Server.EmptyPage));

            Assert.AreEqual("bar", fooHeader);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should accept userDataDir")]
        public async Task ShouldAcceptUserDataDir()
        {
            var (tmp, context, _) = await LaunchAsync();
            Assert.IsNotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());
            await context.CloseAsync();
            Assert.IsNotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());

            tmp.Dispose();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should restore state from userDataDir")]
        public async Task ShouldRestoreStateFromUserDataDir()
        {
            using var userDataDir = new TempDirectory();

            await using (var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path))
            {
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                await page.EvaluateAsync("() => localStorage.hey = 'hello'");
            }

            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                Assert.AreEqual("hello", await page.EvaluateAsync<string>("() => localStorage.hey"));
            }

            using var userDataDir2 = new TempDirectory();
            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir2.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                Assert.That("hello", Is.Not.EqualTo(await page.EvaluateAsync<string>("() => localStorage.hey")));
            }

            userDataDir2.Dispose();
            userDataDir.Dispose();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should restore cookies from userDataDir")]
        [Skip(SkipAttribute.Targets.Chromium | SkipAttribute.Targets.Windows)]
        public async Task ShouldRestoreCookiesFromUserDataDir()
        {
            var userDataDir = new TempDirectory();

            await using (var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path))
            {
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                string documentCookie = await page.EvaluateAsync<string>(@"() => {
                    document.cookie = 'doSomethingOnlyOnce=true; expires=Fri, 31 Dec 9999 23:59:59 GMT';
                    return document.cookie;
                }");

                Assert.AreEqual("doSomethingOnlyOnce=true", documentCookie);
            }

            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                Assert.AreEqual("doSomethingOnlyOnce=true", await page.EvaluateAsync<string>("() => document.cookie"));
            }

            var userDataDir2 = new TempDirectory();
            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir2.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                Assert.That("doSomethingOnlyOnce=true", Is.Not.EqualTo(await page.EvaluateAsync<string>("() => document.cookie")));
            }

            userDataDir2.Dispose();
            userDataDir.Dispose();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should have default URL when launching browser")]
        public async Task ShouldHaveDefaultURLWhenLaunchingBrowser()
        {
            var (tmp, context, page) = await LaunchAsync();

            string[] urls = context.Pages.Select(p => p.Url).ToArray();
            Assert.AreEqual(new[] { "about:blank" }, urls);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should throw if page argument is passed")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldThrowIfPageArgumentIsPassed()
        {
            var tmp = new TempDirectory();
            var args = new[] { Server.EmptyPage };
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
                BrowserType.LaunchPersistentContextAsync(tmp.Path, new() { Args = args }));
            tmp.Dispose();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should have passed URL when launching with ignoreDefaultArgs: true")]
        [Ignore("Skip USES_HOOKS")]
        public void ShouldHavePassedURLWhenLaunchingWithIgnoreDefaultArgsTrue()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should handle timeout")]
        [Ignore("Skip USES_HOOKS")]
        public void ShouldHandleTimeout()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should handle exception")]
        [Ignore("Skip USES_HOOKS")]
        public void ShouldHandleException()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should fire close event for a persistent context")]
        public async Task ShouldFireCloseEventForAPersistentContext()
        {
            var (tmp, context, _) = await LaunchAsync();
            bool closed = false;
            context.Close += (_, _) => closed = true;
            await context.CloseAsync();

            Assert.True(closed);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "coverage should work")]
        [Ignore("We won't support coverage")]
        public void CoverageShouldWork()
        {
            /*
            var (tmp, context, page) = await LaunchAsync();

            await page.Coverage.StartJSCoverageAsync();
            await page.GotoAsync(Server.Prefix + "/jscoverage/simple.html", LoadState.NetworkIdle);
            var coverage = await page.Coverage.StopJSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            StringAssert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.AreEqual(1, coverage[0].Functions.Single(f => f.FunctionName == "foo").Ranges[0].Count);

            tmp.Dispose();
            await context.DisposeAsync();
            */
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "coverage should be missing")]
        [Ignore("We won't support coverage")]
        public void CoverageShouldBeMissing()
        {
            /*
            var (tmp, context, page) = await LaunchAsync();
            Assert.Null(page.Coverage);
            tmp.Dispose();
            await context.DisposeAsync();
            */
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should respect selectors")]
        public async Task ShouldRespectSelectors()
        {
            var (tmp, context, page) = await LaunchAsync();
            const string defaultContextCSS = @"({
                create(root, target) {},
                query(root, selector) {
                    return root.querySelector(selector);
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll(selector));
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "defaultContextCSS", defaultContextCSS);
            await page.SetContentAsync("<div>hello</div>");
            Assert.AreEqual("hello", await page.InnerHTMLAsync("css=div"));
            Assert.AreEqual("hello", await page.InnerHTMLAsync("defaultContextCSS=div"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchAsync(BrowserTypeLaunchPersistentContextOptions options = null)
        {
            var tmp = new TempDirectory();
            var context = await BrowserType.LaunchPersistentContextAsync(tmp.Path, options);
            var page = context.Pages.First();

            return (tmp, context, page);
        }
    }
}
