using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnitTest;
using Microsoft.Playwright.Tests.TestServer;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    public class ContextTestEx : ContextTest
    {
        public SimpleServer Server { get; internal set; }
        public SimpleServer HttpsServer { get; internal set; }

        [SetUp]
        public async Task HttpSetup()
        {
            var http = await HttpService.Register(this);
            Server = http.Server;
            HttpsServer = http.HttpsServer;
        }
    }
}
