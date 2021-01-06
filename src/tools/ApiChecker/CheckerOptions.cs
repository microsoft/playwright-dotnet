using CommandLine;

namespace ApiChecker
{
    [Verb("check-api", isDefault: true)]
    internal class CheckerOptions
    {
        [Option(Required = true, HelpText = "Solution path.")]
        public string BasePath { get; set; }

        [Option(Required = true, HelpText = "Assembly Path.")]
        public string AssemblyPath { get; set; }
    }
}