﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
    public class UserDataDirTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public UserDataDirTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir option</playwright-it>
        [Fact]
        public async Task UserDataDirOption()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.DefaultBrowserOptions;
            options.UserDataDir = userDataDir.Path;

            using var browser = await Playwright.LaunchAsync(options);
            Assert.True(Directory.GetFiles(userDataDir.Path).Length > 0);
            await browser.CloseAsync();
            Assert.True(Directory.GetFiles(userDataDir.Path).Length > 0);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [Fact]
        public async Task UserDataDirArgument()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.DefaultBrowserOptions;

            if (TestConstants.IsFirefox)
            {
                options.Args = options.Args.Concat(new[] { $"--user-data-dir=\"{userDataDir}\"" }).ToArray();
            }
            else
            {
                options.Args = options.Args.Concat(new[] { $"--profile=\"{userDataDir}\"" }).ToArray();
            }

            using var browser = await Playwright.LaunchAsync(options);
            // Open a page to make sure its functional.
            await browser.DefaultContext.NewPageAsync();
            Assert.True(Directory.GetFiles(userDataDir.Path).Length > 0);
            await browser.CloseAsync();
            Assert.True(Directory.GetFiles(userDataDir.Path).Length > 0);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task UserDataDirOptionShouldRestoreState()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.DefaultBrowserOptions;
            options.Args = options.Args.Concat(new[] { $"--user-data-dir=\"{userDataDir}\"" }).ToArray();

            using (var browser = await Playwright.LaunchAsync(options))
            {
                var page = await browser.DefaultContext.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.EvaluateAsync("localStorage.hey = 'hello'");
            }

            using (var browser2 = await Playwright.LaunchAsync(options))
            {
                var page2 = await browser2.DefaultContext.NewPageAsync();
                await page2.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("hello", await page2.EvaluateAsync<string>("localStorage.hey"));
            }
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task UserDataDirOptionShouldRestoreCookies()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.DefaultBrowserOptions;
            options.Args = options.Args.Concat(new[] { $"--user-data-dir=\"{userDataDir}\"" }).ToArray();

            using (var browser = await Playwright.LaunchAsync(options))
            {
                var page = await browser.DefaultContext.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.EvaluateAsync(
                    "document.cookie = 'doSomethingOnlyOnce=true; expires=Fri, 31 Dec 9999 23:59:59 GMT'");
            }

            using (var browser2 = await Playwright.LaunchAsync(options))
            {
                var page2 = await browser2.DefaultContext.NewPageAsync();
                await page2.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("doSomethingOnlyOnce=true", await page2.EvaluateAsync<string>("document.cookie"));
            }
        }
    }
}
