using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAccessibilityTests : PlaywrightSharpPageBaseTest
    {
        public PageAccessibilityTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should work with regular text")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRegularRext()
        {
            await Page.SetContentAsync("<div>Hello World</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Contains("Hello World", JsonSerializer.Serialize(snapshot));
        }
    }
}
