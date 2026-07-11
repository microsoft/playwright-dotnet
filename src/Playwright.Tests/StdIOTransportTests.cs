using System.Diagnostics;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class StdIOTransportTests
{
    [Test]
    public void DecodeMessageSizeShouldRejectInvalidLengths()
    {
        Assert.AreEqual(5, StdIOTransport.DecodeMessageSize(new byte[] { 5, 0, 0, 0 }, 0));

        Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(new byte[] { 0, 0, 0, 0 }, 0));
        Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(new byte[] { 255, 255, 255, 255 }, 0));

        var oversized = BitConverter.GetBytes(StdIOTransport.MaxMessageSize + 1);
        Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(oversized, 0));
    }

    [Test]
    public async Task ScheduleTransportTaskAsyncShouldReturnUnwrappedTask()
    {
        using var cts = new CancellationTokenSource();
        var started = new TaskCompletionSource<object?>();
        var release = new TaskCompletionSource<object?>();

        var task = StdIOTransport.ScheduleTransportTaskAsync(
            async token =>
            {
                started.SetResult(null);
                await release.Task.ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            },
            cts.Token);

        await started.Task.ConfigureAwait(false);
        Assert.False(task.IsCompleted);

        release.SetResult(null);
        await task.ConfigureAwait(false);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Test]
    public void WaitForExitOrKillShouldKillProcessAfterTimeout()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Requires a POSIX shell.");
        }

        string bash;
        try
        {
            bash = SecurityHelpers.GetAbsoluteToolPath("bash");
        }
        catch (PlaywrightException)
        {
            Assert.Ignore("bash is not available.");
            return;
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(bash)
            {
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("while true; do sleep 1; done");

        process.Start();
        var stopwatch = Stopwatch.StartNew();

        var exited = StdIOTransport.WaitForExitOrKill(process, 200);

        stopwatch.Stop();
        Assert.True(exited);
        Assert.True(process.HasExited);
        Assert.Less(stopwatch.Elapsed, TimeSpan.FromSeconds(5));
    }
}
