using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Firefox;

namespace PlaywrightSharp.Tests.DumpIO
{
    class Program
    {
        public const string ChromiumProduct = "CHROMIUM";
        public const string WebkitProduct = "WEBKIT";
        public const string FirefoxProduct = "FIREFOX";

        public static async Task Main(string[] args)
        {
            // Playwright calls a node script, we are going to call ourself.
            if (args[0] == "dump")
            {
                Console.Error.WriteLine("message from dumpio");
                return;
            }

            var options = new LaunchOptions
            {
                IgnoreDefaultArgs = true,
                DumpIO = true,
                ExecutablePath = "dotnet",
                Args =  new [] { "./PlaywrightSharp.Tests.DumpIO.dll", "dump" }
            };

            IBrowserType playwright = args[1] switch
            {
                WebkitProduct => null,
                FirefoxProduct => new FirefoxBrowserType(),
                ChromiumProduct => new ChromiumBrowserType(),
                _ => throw new ArgumentOutOfRangeException($"product {args[1]} does not exist")
            };

            if (args[1] == FirefoxProduct)
            {
                options.Args = options.Args.Append("-juggler").Append("-profile").ToArray();
            }


            var browser = await playwright.LaunchAsync(options);
            await browser.CloseAsync();
        }
    }
}
