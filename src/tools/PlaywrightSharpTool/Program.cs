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
            return Parser.Default.ParseArguments<InstallBrowsersOptions>(args).MapResult(
                async (InstallBrowsersOptions opts) => await InstallAsync(opts),
                _ => Task.FromResult<object>(null));
        }

        private static Task InstallAsync(InstallBrowsersOptions opts) => Playwright.InstallAsync(browsersPath: opts.Path);
    }
}
