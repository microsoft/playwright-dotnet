using System.IO;
using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            (IProtocolTypesGenerator, RevisionInfo)[] generators =
            {
                (new ChromiumProtocolTypesGenerator(), new RevisionInfo
                {
                    ExecutablePath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\PlaywrightSharp.Tests\bin\Debug\netcoreapp3.1\.local-chromium\Win64-733125\chrome-win\chrome.exe"),
                    Local = true,
                }),
            };

            foreach (var (generator, revision) in generators)
            {
                await generator.GenerateTypesAsync(revision).ConfigureAwait(false);
            }
        }
    }
}
