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
                .WithParsed<CheckerOptions>(_ => Checker.Run());
        }
    }
}
