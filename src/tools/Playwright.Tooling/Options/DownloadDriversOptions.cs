using CommandLine;

namespace Playwright.Tooling.Options
{
    [Verb("download-drivers")]
    internal class DownloadDriversOptions
    {
        [Option(Required = true, HelpText = "Solution path.")]
        public string BasePath { get; set; }
    }
}
