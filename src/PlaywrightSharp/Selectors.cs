using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class Selectors
    {
        public Selectors()
        {
        }

        public static Lazy<Selectors> Instance => new Lazy<Selectors>(() => new Selectors());

        public int Generation { get; set; }

        internal async Task<string[]> GetSourcesAsync()
        {
            var sources = new List<string>();

            using var stream = typeof(Selectors).Assembly.GetManifestResourceStream("PlaywrightSharp.Resources.zsSelectorEngineSource.ts");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            sources.Add(await reader.ReadToEndAsync().ConfigureAwait(false));

            return sources.ToArray();
        }
    }
}
