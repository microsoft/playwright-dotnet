using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            await HttpServer.SetupAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            await HttpServer.TeardownAsync();
        }
    }
}
