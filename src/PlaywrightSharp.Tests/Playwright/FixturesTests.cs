using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Playwright
{
    ///<playwright-file>fixtures.spec.js</playwright-file>
    ///<playwright-describe>Fixtures</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class FixturesTests : PlaywrightSharpBaseTest
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
            string dumpioData = string.Empty;
            var process = GetTestAppProcess(
                "PlaywrightSharp.Tests.DumpIO",
                $"\"{BrowserType.CreateBrowserFetcher().GetRevisionInfo().ExecutablePath}\" {TestConstants.Product}");

            process.ErrorDataReceived += (sender, e) =>
            {
                dumpioData += e.Data;
            };

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();
            Assert.Contains("message from dumpio", dumpioData);
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser when the node process closes</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public async Task ShouldCloseTheBrowserWhenTheConnectedProcessCloses()
        {
            int exitCode = await TestSignal(process => KillProcess(process));
            Assert.Equal(TestConstants.IsWindows ? 1 : 0, exitCode);
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

        private async Task<int> TestSignal(Action<Process> killAction)
        {
            string output = string.Empty;
            var browserWebSocketTcs = new TaskCompletionSource<string>();
            var wsMatch = new Regex("browserWS:(.+):browserWS", RegexOptions.Compiled);
            var closeMatch = new Regex("browserClose:([^:]+):browserClose");
            var pidMatch = new Regex("browserPid:([^:]+):browserPid");
            int browserPid = 0;
            int browserExitCode = -1;

            var process = GetTestAppProcess(
                "PlaywrightSharp.Tests.CloseMe",
                $"\"{BrowserType.CreateBrowserFetcher().GetRevisionInfo().ExecutablePath}\" {TestConstants.Product}");

            process.OutputDataReceived += (sender, args) =>
            {
                output += args.Data;

                var match = wsMatch.Match(output);
                if (match.Success)
                {
                    browserWebSocketTcs.TrySetResult(match.Groups[1].Value);
                }

                match = closeMatch.Match(output);
                if (match.Success)
                {
                    browserExitCode = Convert.ToInt32(match.Groups[1].Value);
                }

                match = pidMatch.Match(output);
                if (match.Success)
                {
                    browserPid = Convert.ToInt32(match.Groups[1].Value);
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            var browser = await BrowserType.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = await browserWebSocketTcs.Task
            });

            var browserDisconnectedTcs = new TaskCompletionSource<bool>();
            var processExitedTcs = new TaskCompletionSource<bool>();

            browser.Disconnected += (sender, e) => processExitedTcs.TrySetResult(true);

            killAction(process);
            process.WaitForExit();
            await browserDisconnectedTcs.Task.WithTimeout();

            return browserExitCode;
        }

        private void KillProcess(Process process)
        {
            //We need to kill the process tree manually
            //See: https://github.com/dotnet/corefx/issues/26234
#if NETCOREAPP3_1
            process.Kill(true);
#else
            var killerProcess = new Process();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                killerProcess.StartInfo.FileName = "taskkill";
                killerProcess.StartInfo.Arguments = $"-pid {process.Id} -t -f";
            }
            else
            {
                killerProcess.StartInfo.FileName = "/bin/bash";
                killerProcess.StartInfo.Arguments = $"-c \"kill {process.Id}\"";
            }
            killerProcess.Start();
            killerProcess.WaitForExit();
#endif
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
            process.StartInfo.RedirectStandardOutput = true;
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
