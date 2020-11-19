using System;
using System.Threading.Tasks;
using CommandLine;
using static ApiChecker.ScaffoldTest;

namespace ApiChecker
{
    static class Program
    {
        static Task Main(string[] args)
        {
            return Parser.Default.ParseArguments<ScaffoldTestOptions, CheckerOptions>(args).MapResult(
                async (ScaffoldTestOptions opts) => await DoScaffoldTest(opts),
                async (CheckerOptions opts) => await Checker.Run(),
                _ => Task.CompletedTask
                );
        }

        private static Task DoScaffoldTest(ScaffoldTestOptions opts) => ScaffoldTest.Run(opts);
    }
}
