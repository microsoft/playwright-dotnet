using System;
using CommandLine;

namespace PlaywrightSharpTool.Options
{
    [Verb("install-driver", HelpText = "Install the playwright driver. Pass --path to specify the driver location.")]
    public class InstallDriverOptions
    {
        [Option(Required = false, HelpText = "Driver destination path. If not passed, CWD will be used.")]
        public string Path { get; set; }
    }
}
