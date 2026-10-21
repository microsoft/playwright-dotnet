using System.Diagnostics;
using System.Globalization;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Tests;

[NonParallelizable]
public class StdIOTransportShutdownTests
{
    [TestCase(128, false)]
    [TestCase(1024 * 1024, false)]
    [TestCase(1024 * 1024, true)]
    public Task ShouldDrainDriverOutputDuringClose(int payloadSize, bool closeFromCallback)
        => WithDriverAsync($$"""
            process.stdin.once('data', () => {
                process.stdout.write(Buffer.from([1, 0, 0, 0, 0]));
            });
            process.stdin.resume();
            process.stdin.on('end', () => {
                const frame = Buffer.alloc({{payloadSize}} + 4);
                frame.writeUInt32LE({{payloadSize}}, 0);
                for (let i = 0; i < 16; ++i)
                    process.stdout.write(frame);
                process.stdout.end();
            });
            """, async (transport, driver) =>
            {
                var callbackClose = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var messagesAfterClose = 0;
                transport.TransportClosed += (_, reason) =>
                {
                    transport.Close(reason);
                    transport.Dispose();
                };
                transport.MessageReceived += (_, _) =>
                {
                    if (transport.IsClosed)
                    {
                        Interlocked.Increment(ref messagesAfterClose);
                    }
                    else if (closeFromCallback)
                    {
                        try
                        {
                            transport.Close(new TargetClosedException("Callback shutdown"));
                            callbackClose.TrySetResult(true);
                        }
                        catch (Exception exception)
                        {
                            callbackClose.TrySetException(exception);
                        }
                    }
                };
                Task close;
                if (closeFromCallback)
                {
                    close = callbackClose.Task;
                    await transport.SendAsync(new byte[] { 0 });
                }
                else
                {
                    close = Task.Run(() => transport.Close(new TargetClosedException("Controlled shutdown")));
                }
                Assert.That(await Task.WhenAny(close, Task.Delay(TimeSpan.FromSeconds(10))), Is.SameAs(close),
                    "Closing the driver must drain its final protocol output, including from a message callback.");
                await close;
                Assert.That(driver.HasExited, Is.True);
                Assert.That(driver.ExitCode, Is.Zero);
                Assert.That(messagesAfterClose, Is.Zero, "Closed transport must discard final protocol messages.");
            });

    [Test]
    public Task ShouldCloseConnectionWhenDriverClosesOutput()
        => WithDriverAsync("""
            process.stdin.once('data', () => process.exit(0));
            process.stdin.resume();
            process.stdin.on('end', () => process.exit(0));
            """, async (transport, driver) =>
            {
                var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                transport.TransportClosed += (_, reason) =>
                {
                    try
                    {
                        // The production connection callback closes and disposes the transport.
                        transport.Close(reason);
                        transport.Dispose();
                        closed.TrySetResult(true);
                    }
                    catch (Exception exception)
                    {
                        closed.TrySetException(exception);
                    }
                };
                await transport.SendAsync(new byte[] { 0 });
                Assert.That(await Task.WhenAny(closed.Task, Task.Delay(TimeSpan.FromSeconds(10))), Is.SameAs(closed.Task));
                await closed.Task;
                Assert.That(transport.IsClosed, Is.True);
                Assert.That(driver.HasExited, Is.True);
                Assert.That(driver.ExitCode, Is.Zero);
            });

    private static async Task WithDriverAsync(string script, Func<StdIOTransport, Process, Task> action)
    {
        var (node, _) = Driver.GetExecutablePath();
        var directory = Path.Combine(Path.GetTempPath(), "playwright-shutdown-" + Guid.NewGuid().ToString("N"));
        StdIOTransport transport = null;
        Process driver = null;
        try
        {
            var package = Path.Combine(directory, ".playwright", "package");
            Directory.CreateDirectory(package);
            var pidFile = Path.Combine(package, "pid");
            await File.WriteAllTextAsync(Path.Combine(package, "cli.js"), """
                const fs = require('fs');
                const path = require('path');
                fs.writeFileSync(path.join(__dirname, 'pid.tmp'), String(process.pid));
                fs.renameSync(path.join(__dirname, 'pid.tmp'), path.join(__dirname, 'pid'));
                """ + Environment.NewLine + script);
            var originalSearchPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
            var originalNode = Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH");
            try
            {
                Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", directory);
                Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", node);
                transport = new StdIOTransport();
            }
            finally
            {
                Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", originalSearchPath);
                Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", originalNode);
            }
            using var startup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!File.Exists(pidFile))
            {
                await Task.Delay(10, startup.Token);
            }
            driver = Process.GetProcessById(int.Parse(await File.ReadAllTextAsync(pidFile), CultureInfo.InvariantCulture));
            _ = driver.SafeHandle;
            await action(transport, driver);
        }
        finally
        {
            try
            {
                if (driver != null && !driver.HasExited)
                {
                    driver.Kill();
                    driver.WaitForExit();
                }
                if (transport != null && !transport.IsClosed)
                {
                    transport.Close(new TargetClosedException("Test cleanup"));
                }
            }
            finally
            {
                driver?.Dispose();
                transport?.Dispose();
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }
    }
}
