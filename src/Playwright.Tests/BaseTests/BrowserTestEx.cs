using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    public class BrowserTestEx : BrowserTest
    {
        [TearDown]
        public void ServerTeardown()
        {
            HttpServer.Server.Reset();
            HttpServer.HttpsServer.Reset();
        }
    }
}
