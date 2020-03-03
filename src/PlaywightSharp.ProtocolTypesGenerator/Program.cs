using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            (IProtocolTypesGenerator, RevisionInfo)[] generators =
            {
                (new ChromiumProtocolTypesGenerator(), new RevisionInfo()),
            };

            foreach (var (generator, revision) in generators)
            {
                await generator.GenerateTypesAsync(revision).ConfigureAwait(false);
            }
        }
    }
}
