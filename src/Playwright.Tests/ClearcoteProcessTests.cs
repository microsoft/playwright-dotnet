using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ClearcoteProcessTests
{
    [Test]
    public void CreateServeProcessStartInfoShouldNotRedirectOutputPipes()
    {
        var startInfo = Clearcote.CreateServeProcessStartInfo(
            OperatingSystem.IsWindows() ? @"C:\browser\chrome.exe" : "/browser/chrome",
            new[] { "--flag=value with spaces", "--quoted=\"value\"" },
            new Dictionary<string, string?> { ["CLEARCOTE_TEST_ENV"] = "1" });

        Assert.False(startInfo.UseShellExecute);
        Assert.False(startInfo.RedirectStandardOutput);
        Assert.False(startInfo.RedirectStandardError);
        CollectionAssert.AreEqual(new[] { "--flag=value with spaces", "--quoted=\"value\"" }, startInfo.ArgumentList);
        Assert.AreEqual("1", startInfo.EnvironmentVariables["CLEARCOTE_TEST_ENV"]);
    }

    [Test]
    public async Task RunProcessShouldDrainStdoutAndStderrConcurrently()
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

        var result = await Clearcote.RunProcessAsync(
            bash,
            "-c",
            "for i in {1..5000}; do printf '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789\\n' >&2; done; printf 'done\\n'");

        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("done\n", result.Stdout);
        Assert.Greater(result.Stderr.Length, 300_000);
    }
}
