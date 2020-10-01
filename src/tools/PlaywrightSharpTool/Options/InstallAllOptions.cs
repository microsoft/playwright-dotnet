using System;
using CommandLine;

namespace PlaywrightSharpTool.Options
{
    [Verb("install-all", HelpText = "Install the playwright driver and the required browsers. Pass --driverpath to specify the driver location and --browserpath to specify the location of the browsers.")]
    public class InstallAllOptions
    {
        [Option(Required = false, HelpText = "Browsers destination path. If not passed, the default shared location will be used.")]
        public string DriverPath { get; set; }

        [Option(Required = false, HelpText = "Driver destination path. If not passed, CWD will be used.")]
        public string BrowsersPath { get; set; }
    }
}
