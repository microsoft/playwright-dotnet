using System;
using System.Threading.Tasks;
using CommandLine;
using PlaywrightSharp;
using PlaywrightSharpTool.Options;

namespace PlaywrightSharpTool
{
    class Program
    {
        static Task Main(string[] args)
        {
            return Parser.Default.ParseArguments<InstallBrowsersOptions, InstallDriverOptions, InstallAllOptions>(args).MapResult(
                async (InstallBrowsersOptions opts) => await InstallAsync(opts),
                async (InstallDriverOptions opts) => await InstallDriverAsync(opts),
                async (InstallAllOptions opts) => await InstallAllDriverAsync(opts),
                _ => Task.FromResult<object>(null));
        }

        private static async Task InstallAllDriverAsync(InstallAllOptions opts)
        {
            opts.DriverPath ??= ".";

            Playwright.InstallDriver(opts.DriverPath);
            await Playwright.InstallAsync(opts.DriverPath, opts.BrowsersPath);
        }

        private static Task InstallDriverAsync(InstallDriverOptions opts)
        {
            Playwright.InstallDriver(opts.Path ?? ".");
            return Task.CompletedTask;
        }

        private static Task InstallAsync(InstallBrowsersOptions opts) => Playwright.InstallAsync(browsersPath: opts.Path);
    }
}
