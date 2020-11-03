using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Issues
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class Issue983 : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public Issue983(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync("https://github.com");
            string title = await Page.GetTitleAsync();
            Assert.Contains("GitHub", title);
        }
    }
}
