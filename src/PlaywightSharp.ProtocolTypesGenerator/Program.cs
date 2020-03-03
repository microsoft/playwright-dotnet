using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            IProtocolTypesGenerator[] generators =
            {
                new ChromiumProtocolTypesGenerator(),
            };

            var revision = new RevisionInfo("test")
            {
                ExecutablePath = @"D:\Playground\Playground.cs\ConsoleApp1\ConsoleApp1\.local-chromium\Win64-706915\chrome-win\chrome.exe",
                Local = true,
            };

            foreach (var generator in generators)
            {
                await generator.GenerateTypesAsync(revision).ConfigureAwait(false);
            }
        }
    }
}
