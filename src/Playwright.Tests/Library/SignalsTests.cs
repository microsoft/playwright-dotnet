/**
 * Copyright 2019 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Playwright.Tests.Fixtures;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/signals.spec.ts.</summary>
[ItSlow]
public class SignalsTests
{
    [UpstreamTest("SI-001 should close the browser when the node process closes")]
    public async Task ShouldCloseTheBrowserWhenTheNodeProcessCloses(StartRemoteServer startRemoteServer, bool isWindows, Server server, string mode)
    {
        var remoteServer = await startRemoteServer("launchServer", new() { Url = server.EmptyPage });
        try
        {
            if (isWindows)
            {
                // .NET: execSync(`taskkill /pid ${remoteServer.child().pid} /T /F`, { stdio: 'ignore' })
                using var taskkill = Process.Start(new ProcessStartInfo("taskkill", $"/pid {remoteServer.Child().Id} /T /F") { UseShellExecute = false, CreateNoWindow = true });
                await taskkill.WaitForExitAsync();
                if (taskkill.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Command failed: taskkill /pid {remoteServer.Child().Id} /T /F");
                }
            }
            else
            {
                NodeProcess.Kill(remoteServer.Child().Id);
            }
        }
        catch (Exception)
        {
            // console.log(error);
            // if (error.stdout)
            //   console.log('--- stdout ---\n', error.stdout);
            // if (error.stderr)
            //   console.log('--- stderr ---\n', error.stderr);
            throw;
        }
        // We might not get browser exitCode in time when killing the parent node process,
        // so we don't check it here.
        Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(isWindows ? 1 : 0));
    }

    [UpstreamTest("SI-002 should remove temp dir on process.exit")]
    public async Task ShouldRemoveTempDirOnProcessExit(StartRemoteServer startRemoteServer, Server server, string platform, TestInfo testInfo)
    {
        It.Skip(platform == "win32", "Removing user data dir synchronously is blocked on Windows");

        var file = testInfo.OutputPath("exit.file");
        var remoteServer = await startRemoteServer("launchServer", new() { Url = server.EmptyPage, ExitOnFile = file });
        var tempDir = await remoteServer.Out("tempDir");
        var before = Directory.Exists(tempDir);
        File.WriteAllText(file, "data");
        Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(42));
        var after = Directory.Exists(tempDir);
        Assert.That(before, Is.EqualTo(true));
        Assert.That(after, Is.EqualTo(false));
    }

    public class Signals
    {
        [BeforeEach]
        public static void Skip(string platform) => It.Skip(platform == "win32");

        [UpstreamTest("SI-003 should report browser close signal 2")]
        public async Task ShouldReportBrowserCloseSignal2(StartRemoteServer startRemoteServer, Server server, bool isMac, int macVersion, string browserName)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { Url = server.EmptyPage });
            var pid = await remoteServer.Out("pid");
            NodeProcess.Kill(-int.Parse(pid, CultureInfo.InvariantCulture), "SIGKILL");
            if (isMac && browserName == "webkit" && macVersion > 13 && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                // WebKit on newer macOS exits sometimes with exit code, sometimes with signal.
                Assert.That(
                    "exitCode:" + await remoteServer.Out("exitCode") +
                    "signal:" + await remoteServer.Out("signal"), Does.Match("exitCode:137|signal:SIGKILL"));
            }
            else
            {
                Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("null"));
                Assert.That(await remoteServer.Out("signal"), Is.EqualTo("SIGKILL"));
            }
            NodeProcess.Kill(remoteServer.Child().Id);
            await remoteServer.ChildExitCode();
        }

        [UpstreamTest("SI-004 should close the browser on SIGINT")]
        public async Task ShouldCloseTheBrowserOnSIGINT(StartRemoteServer startRemoteServer, Server server)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { Url = server.EmptyPage });
            NodeProcess.Kill(remoteServer.Child().Id, "SIGINT");
            Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("0"));
            Assert.That(await remoteServer.Out("signal"), Is.EqualTo("null"));
            Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(130));
        }

        [UpstreamTest("SI-005 should close the browser on SIGTERM")]
        public async Task ShouldCloseTheBrowserOnSIGTERM(StartRemoteServer startRemoteServer, Server server)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { Url = server.EmptyPage });
            NodeProcess.Kill(remoteServer.Child().Id, "SIGTERM");
            Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("0"));
            Assert.That(await remoteServer.Out("signal"), Is.EqualTo("null"));
            Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(0));
        }

        [UpstreamTest("SI-006 should close the browser on SIGHUP")]
        public async Task ShouldCloseTheBrowserOnSIGHUP(StartRemoteServer startRemoteServer, Server server)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { Url = server.EmptyPage });
            NodeProcess.Kill(remoteServer.Child().Id, "SIGHUP");
            Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("0"));
            Assert.That(await remoteServer.Out("signal"), Is.EqualTo("null"));
            Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(0));
        }

        [UpstreamTest("SI-007 should kill the browser on double SIGINT and remove temp dir")]
        public async Task ShouldKillTheBrowserOnDoubleSIGINTAndRemoveTempDir(StartRemoteServer startRemoteServer, Server server)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { StallOnClose = true, Url = server.EmptyPage });
            var tempDir = await remoteServer.Out("tempDir");
            var before = Directory.Exists(tempDir);
            NodeProcess.Kill(remoteServer.Child().Id, "SIGINT");
            await remoteServer.Out("stalled");
            NodeProcess.Kill(remoteServer.Child().Id, "SIGINT");
            Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("null"));
            Assert.That(await remoteServer.Out("signal"), Is.EqualTo("SIGKILL"));
            Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(130));
            var after = Directory.Exists(tempDir);
            Assert.That(before, Is.EqualTo(true));
            Assert.That(after, Is.EqualTo(false));
        }

        [UpstreamTest("SI-008 should kill the browser on SIGINT + SIGTERM")]
        public async Task ShouldKillTheBrowserOnSIGINTSIGTERM(StartRemoteServer startRemoteServer, Server server)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { StallOnClose = true, Url = server.EmptyPage });
            NodeProcess.Kill(remoteServer.Child().Id, "SIGINT");
            await remoteServer.Out("stalled");
            NodeProcess.Kill(remoteServer.Child().Id, "SIGTERM");
            Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("null"));
            Assert.That(await remoteServer.Out("signal"), Is.EqualTo("SIGKILL"));
            Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(0));
        }

        [UpstreamTest("SI-009 should kill the browser on SIGTERM + SIGINT")]
        public async Task ShouldKillTheBrowserOnSIGTERMSIGINT(StartRemoteServer startRemoteServer, Server server)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { StallOnClose = true, Url = server.EmptyPage });
            NodeProcess.Kill(remoteServer.Child().Id, "SIGTERM");
            await remoteServer.Out("stalled");
            NodeProcess.Kill(remoteServer.Child().Id, "SIGINT");
            Assert.That(await remoteServer.Out("exitCode"), Is.EqualTo("null"));
            Assert.That(await remoteServer.Out("signal"), Is.EqualTo("SIGKILL"));
            Assert.That(await remoteServer.ChildExitCode(), Is.EqualTo(130));
        }

        [UpstreamTest("SI-010 should not prevent default SIGTERM handling after browser close")]
        public async Task ShouldNotPreventDefaultSIGTERMHandlingAfterBrowserClose(StartRemoteServer startRemoteServer, Server server, string platform, TestInfo testInfo)
        {
            var remoteServer = await startRemoteServer("launchServer", new() { StartStopAndRunHttp = true });
            Assert.That(await remoteServer.Out("closed"), Is.EqualTo("success"));
            NodeProcess.Kill(remoteServer.Child().Id, "SIGTERM");
            Assert.That(await remoteServer.ChildExitCode(), Is.Null);
            Assert.That(await remoteServer.ChildSignal(), Is.EqualTo("SIGTERM"));
        }
    }
}
