using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    internal class TestUtils
    {
        internal static string FindParentDirectory(string directory)
        {
            // when using Directory.GetCurrentDirectory, for .NET 4.8 under test explorer on Windows,
            // the location was Local App Data
            string current = AppContext.BaseDirectory;
            while (!Directory.Exists(Path.Combine(current, directory)))
            {
                current = Directory.GetParent(current).FullName;
            }
            return Path.Combine(current, directory);
        }

        internal static void AssertSSLError(string errorMessage)
        {
            if (TestConstants.IsChromium)
            {
                StringAssert.Contains("net::ERR_CERT_AUTHORITY_INVALID", errorMessage);
            }
            else if (TestConstants.IsWebKit)
            {
                if (TestConstants.IsMacOSX)
                    StringAssert.Contains("The certificate for this server is invalid", errorMessage);
                else if (TestConstants.IsWindows)
                    StringAssert.Contains("SSL peer certificate or SSH remote key was not OK", errorMessage);
                else
                    StringAssert.Contains("Unacceptable TLS certificate", errorMessage);
            }
            else
            {
                StringAssert.Contains("SSL_ERROR_UNKNOWN", errorMessage);
            }
        }

        internal static async Task RegisterEngineWithPathAsync(IPlaywright playwright, string name, string path)
        {
            try
            {
                await playwright.Selectors.RegisterAsync(name, new() { Path = path });
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("has been already registered"))
            {
            }
        }

        internal static string GetWebServerFile(string path) => Path.Combine(FindParentDirectory("Playwright.Tests.TestServer"), "wwwroot", path);

        internal static async Task VerifyViewportAsync(IPage page, int width, int height)
        {
            Assert.AreEqual(width, (int)page.ViewportSize.Width);
            Assert.AreEqual(height, (int)page.ViewportSize.Height);
            Assert.AreEqual(width, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.AreEqual(height, await page.EvaluateAsync<int>("window.innerHeight"));
        }

        internal static async Task RegisterEngineAsync(IPlaywright playwright, string name, string script, bool? contentScript = null)
        {
            try
            {
                await playwright.Selectors.RegisterAsync(name, new() { Script = script, ContentScript = contentScript });
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("has been already registered"))
            {
            }
        }
    }
}
