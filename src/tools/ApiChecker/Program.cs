using System;
using System.Threading.Tasks;
using CommandLine;

namespace ApiChecker
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ScaffoldTestOptions, CheckerOptions>(args)
                .WithParsed<ScaffoldTestOptions>(o => ScaffoldTest.Run(o))
                .WithParsed<CheckerOptions>(o => RunApiChecker(o));
        }

        private static void RunApiChecker(CheckerOptions o)
        {
            var checker = new PlaywrightSharp.BuildTasks.ApiChecker
            {
                BasePath = o.BasePath,
                AssemblyPath = o.AssemblyPath,
                IsBuildTask = false,
            };
            checker.Execute();
        }
    }
}
