using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Playwright
{
    ///<playwright-file>fixtures.spec.js</playwright-file>
    ///<playwright-describe>Fixtures</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FixturesTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public FixturesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>dumpio option should work with webSocket option</playwright-it>
        [Fact(Skip = "It will always be with websockets")]
        public void DumpioOptionShouldWorkWithWebSocketOption() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should dump browser process stderr</playwright-it>
        [Retry]
        public void ShouldDumpBrowserProcessStderr()
        {
            bool success = false;
            var process = GetTestAppProcess(
                "PlaywrightSharp.Tests.DumpIO",
                $"\"{Playwright.CreateBrowserFetcher().GetRevisionInfo().ExecutablePath}\" {TestConstants.Product}");

            process.ErrorDataReceived += (sender, e) =>
            {
                success |= e.Data != null && e.Data.Contains("DevTools listening on ws://");
            };

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();
            Assert.True(success);
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser when the node process closes</playwright-it>
        [Retry]
        public async Task ShouldCloseTheBrowserWhenTheConnectedProcessCloses()
        {
            var browserApp = await TestSignal(process => KillProcess(process.Id));
            Assert.Equal(TestConstants.IsWindows ? 1 : 0, browserApp.Process.ExitCode);
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should report browser close signal</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldReportBrowserCloseSignal() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should report browser close signal 2</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldReportBrowserCloseSignal2() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGINT</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGTERM</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGHUP</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should kill the browser on double SIGINT</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should kill the browser on SIGINT + SIGTERM</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should kill the browser on SIGTERM + SIGINT</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGTERMAndSIGINT() { }

        private async Task<IBrowserApp> TestSignal(Action<Process> killAction)
        {
            var browserClosedTcs = new TaskCompletionSource<bool>();
            var processExitedTcs = new TaskCompletionSource<bool>();
            var browserApp = await Playwright.LaunchBrowserAppAsync();

            browserApp.Process.Exited += (sender, e) => processExitedTcs.TrySetResult(true);

            var browser = await Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = browserApp.WebSocketEndpoint
            });

            browser.Disconnected += (sender, e) =>
            {
                browserClosedTcs.SetResult(true);
            };

            killAction(browserApp.Process);

            await Task.WhenAll(browserClosedTcs.Task, processExitedTcs.Task);

            return browserApp;
        }

        private void KillProcess(int pid)
        {
            var process = new Process();

            //We need to kill the process tree manually
            //See: https://github.com/dotnet/corefx/issues/26234
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "taskkill";
                process.StartInfo.Arguments = $"-pid {pid} -t -f";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"kill {pid}\"";
            }

            process.Start();
            process.WaitForExit();
        }

        private Process GetTestAppProcess(string appName, string arguments)
        {
            var process = new Process();

#if NETCOREAPP
            process.StartInfo.WorkingDirectory = GetSubprocessWorkingDir(appName);
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"{appName}.dll {arguments}";
#else
            process.StartInfo.FileName = Path.Combine(GetSubprocessWorkingDir(appName), $"{appName}.exe");
            process.StartInfo.Arguments = arguments;
#endif
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            return process;
        }

        private string GetSubprocessWorkingDir(string dir)
        {
#if DEBUG
            string build = "Debug";
#else

            var build = "Release";
#endif
#if NETCOREAPP
            return Path.Combine(
                TestUtils.FindParentDirectory("src"),
                dir,
                "bin",
                build,
                "netcoreapp3.1");
#else
            return Path.Combine(
                TestUtils.FindParentDirectory("src"),
                dir,
                "bin",
                build,
                "net48");
#endif
        }
    }
}
