using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageScrollTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageScrollTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
