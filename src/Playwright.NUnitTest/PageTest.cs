using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Microsoft.Playwright.NUnitTest
{
    public class PageTest : ContextTest
    {
        public IPage Page { get; private set; }

        [SetUp]
        public async Task PageSetup()
        {
            Page = await Context.NewPageAsync();
        }
    }
}
