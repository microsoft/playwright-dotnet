using System.Threading.Tasks;
using CommandLine;

namespace ApiChecker
{
    internal static class Program
    {
        internal static Task Main(string[] args)
        {
            return Parser.Default.ParseArguments<ScaffoldTestOptions, CheckerOptions>(args).MapResult(
                async (ScaffoldTestOptions opts) => await DoScaffoldTest(opts).ConfigureAwait(false),
                async (CheckerOptions opts) => await Checker.Run().ConfigureAwait(false),
                _ => Task.CompletedTask);
        }

        private static Task DoScaffoldTest(ScaffoldTestOptions opts) => ScaffoldTest.Run(opts);
    }
}
