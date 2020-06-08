using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    internal class Selectors : ISelectors
    {
        private readonly List<string> _sources = new List<string>();

        private Selectors()
        {
            _sources.Add(GetZsSelectorEngineSource());
        }

        public static Lazy<Selectors> Instance { get; } = new Lazy<Selectors>(() => new Selectors());

        public int Generation { get; private set; }

        public IEnumerable<string> Sources => _sources;

        public Task RegisterAsync(string engineFunction, params object[] args)
        {
            string source = Page.GetEvaluationString(engineFunction, args);
            _sources.Add(source);
            Generation++;

            return Task.CompletedTask;
        }

        public async Task<string> CreateSelectorAsync(string name, IElementHandle elementHandle)
        {
            var mainContext = await ((ElementHandle)elementHandle).Page.MainFrame.GetMainContextAsync().ConfigureAwait(false);
            return await mainContext.EvaluateAsync<string>(
                @"(injected, target, name) => {
                    return injected.engines.get(name).create(document.documentElement, target);
                }",
                await mainContext.GetInjectedAsync().ConfigureAwait(false),
                elementHandle,
                name).ConfigureAwait(false);
        }

        private static string GetZsSelectorEngineSource()
        {
            using var stream = typeof(Selectors).Assembly.GetManifestResourceStream("PlaywrightSharp.Resources.zsSelectorEngineSource.ts");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
