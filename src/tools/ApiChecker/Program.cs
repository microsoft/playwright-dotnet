using System.Threading.Tasks;
using CommandLine;

namespace ApiChecker
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ScaffoldTestOptions>(args)
                .WithParsed<ScaffoldTestOptions>(ScaffoldTest.Run);
        }
    }
}
