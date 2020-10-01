using System;
using CommandLine;

namespace PlaywrightSharpTool.Options
{
    [Verb("install-browsers", HelpText = "Install all the browsers used by Playwright Sharp. Pass --path to specify the location of the browsers.")]
    public class InstallBrowsersOptions
    {
        [Option(Required = false, HelpText = "Browsers destination path. If not passed, the default shared location will be used.")]
        public string Path { get; set; }
    }
}
