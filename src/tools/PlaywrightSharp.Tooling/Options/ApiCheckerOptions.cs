using CommandLine;

namespace PlaywrightSharp.Tooling.Options
{
    [Verb("check-api")]
    internal class ApiCheckerOptions
    {
        [Option(Required = true, HelpText = "Solution path.")]
        public string BasePath { get; set; }
    }
}
