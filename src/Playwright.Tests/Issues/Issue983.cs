using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests.Issues
{
    /// <summary>
    /// See https://github.com/microsoft/playwright-sharp/issues/983.
    /// </summary>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class Issue983 : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public Issue983(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// See https://github.com/microsoft/playwright-sharp/issues/983.
        /// </summary>
        [Fact(Skip = "Getting navigation timeouts on CI")]
        public async Task ShouldWork()
        {
            await Page.GotoAsync("https://github.com");
            string title = await Page.TitleAsync();
            Assert.Contains("GitHub", title);
        }
    }
}
