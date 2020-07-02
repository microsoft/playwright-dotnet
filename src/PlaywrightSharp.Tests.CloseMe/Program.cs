using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.CloseMe
{
    class Program
    {
        public const string ChromiumProduct = "CHROMIUM";
        public const string WebkitProduct = "WEBKIT";
        public const string FirefoxProduct = "FIREFOX";

        static async Task Main(string[] args)
        {
            var options = new LaunchOptions
            {
                ExecutablePath = args[0],
            };

            IBrowserType playwright = args[1] switch
            {
                WebkitProduct => null,
                FirefoxProduct => new FirefoxBrowserType(),
                ChromiumProduct => new ChromiumBrowserType(),
                _ => throw new ArgumentOutOfRangeException($"product {args[1]} does not exist")
            };

            var browserApp = await playwright.LaunchBrowserAppAsync(options);

            browserApp.Closed += (sender, eventArgs) => Console.WriteLine($"browserClose:{browserApp.Process.ExitCode}:browserClose");

            Console.WriteLine($"browserPid:{browserApp.Process.Id}:browserPid");
            Console.WriteLine($"browserWS:{browserApp.WebSocketEndpoint}:browserWS");
            Console.ReadLine();
        }
    }
}
