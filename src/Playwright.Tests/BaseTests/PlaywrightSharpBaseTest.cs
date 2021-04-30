using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Helpers;
using Microsoft.Playwright.Tests.TestServer;
using Microsoft.Playwright.Transport.Channels;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests.BaseTests
{
    /// <summary>
    /// This base tests setup logging and http servers
    /// </summary>
    public class PlaywrightSharpBaseTest : IDisposable
    {
        private readonly XunitLoggerProvider _loggerProvider;
        private readonly ILogger<SimpleServer> _httpLogger;

        internal IPlaywright Playwright => PlaywrightSharpBrowserLoaderFixture.Playwright;
        internal string BaseDirectory { get; set; }
        internal IBrowserType BrowserType => Playwright[TestConstants.Product];

        internal SimpleServer Server => PlaywrightSharpLoader.Server;
        internal SimpleServer HttpsServer => PlaywrightSharpLoader.HttpsServer;

        internal PlaywrightSharpBaseTest(ITestOutputHelper output)
        {
            BaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
            var dirInfo = new DirectoryInfo(BaseDirectory);

            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            Initialize();

            _loggerProvider = new XunitLoggerProvider(output);
            _httpLogger = TestConstants.LoggerFactory.CreateLogger<SimpleServer>();
            TestConstants.LoggerFactory.AddProvider(_loggerProvider);
            Server.RequestReceived += Server_RequestReceived;
            HttpsServer.RequestReceived += Server_RequestReceived;

            output.WriteLine($"Running {GetDisplayName(output)}");
        }

        private void Server_RequestReceived(object sender, RequestReceivedEventArgs e)
        {
            _httpLogger.LogInformation($"Incoming request: {e.Request.Path}");
        }

        private static string GetDisplayName(ITestOutputHelper output)
        {
            var type = output.GetType();
            var testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            var test = (ITest)testMember.GetValue(output);
            return test.DisplayName;
        }

        internal void Initialize()
        {
            Server.Reset();
            HttpsServer.Reset();
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Server.RequestReceived -= Server_RequestReceived;
            HttpsServer.RequestReceived -= Server_RequestReceived;
            _loggerProvider.Dispose();
        }
    }
}
