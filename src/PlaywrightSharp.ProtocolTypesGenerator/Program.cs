using System;
using System.Threading.Tasks;
using PlaywrightSharp.ProtocolTypesGenerator.Chromium;
using PlaywrightSharp.ProtocolTypesGenerator.Firefox;
using PlaywrightSharp.ProtocolTypesGenerator.Webkit;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            IBrowserProtocolTypesGenerator[] generators =
            {
                new ChromiumBrowserProtocolTypesGenerator(),
                new FirefoxBrowserProtocolTypesGenerator(),
                new WebkitBrowserProtocolTypesGenerator(),
            };

            foreach (var generator in generators)
            {
                int percentage = 0;
                generator.BrowserFetcher.DownloadProgressChanged += (sender, e) =>
                {
                    if (percentage != e.ProgressPercentage)
                    {
                        percentage = e.ProgressPercentage;
                        Console.WriteLine($"[{generator.GetType().Name}] downloading browser {percentage}%");
                    }
                };
                var revision = await generator.BrowserFetcher.DownloadAsync().ConfigureAwait(false);
                await generator.ProtocolTypesGenerator.GenerateCodeAsync(revision).ConfigureAwait(false);
            }
        }
    }
}
