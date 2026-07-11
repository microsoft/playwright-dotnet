using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ClearcoteExecutablePathTests
{
    [Test]
    public void ExecutablePathAsyncShouldRejectRelativeExplicitPath()
    {
        using var tmpDir = new TempDirectory();
        var oldCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);
            File.WriteAllText(OperatingSystem.IsWindows() ? "chrome.exe" : "chrome", string.Empty);

            var exception = Assert.ThrowsAsync<PlaywrightException>(
                async () => await Clearcote.ExecutablePathAsync(OperatingSystem.IsWindows() ? "chrome.exe" : "chrome", null, quiet: true, autoUpdate: false));

            StringAssert.Contains("Clearcote executable path must be a fully-qualified path", exception.Message);
        }
        finally
        {
            Directory.SetCurrentDirectory(oldCwd);
        }
    }

    [Test]
    public async Task ExecutablePathAsyncShouldReturnCanonicalExplicitPath()
    {
        using var tmpDir = new TempDirectory();
        var executablePath = Path.Combine(tmpDir.Path, OperatingSystem.IsWindows() ? "chrome.exe" : "chrome");
        File.WriteAllText(executablePath, string.Empty);

        var resolved = await Clearcote.ExecutablePathAsync(executablePath, null, quiet: true, autoUpdate: false);

        Assert.AreEqual(Path.GetFullPath(executablePath), resolved);
    }

    [Test]
    public void ExecutablePathAsyncShouldRejectRelativeEnvironmentBinary()
    {
        using var tmpDir = new TempDirectory();
        var oldCwd = Directory.GetCurrentDirectory();
        var originalBinary = Environment.GetEnvironmentVariable("CLEARCOTE_BINARY");
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);
            File.WriteAllText(OperatingSystem.IsWindows() ? "chrome.exe" : "chrome", string.Empty);
            Environment.SetEnvironmentVariable("CLEARCOTE_BINARY", OperatingSystem.IsWindows() ? "chrome.exe" : "chrome");

            var exception = Assert.ThrowsAsync<PlaywrightException>(
                async () => await Clearcote.ExecutablePathAsync(null, null, quiet: true, autoUpdate: false));

            StringAssert.Contains("CLEARCOTE_BINARY must be a fully-qualified path", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLEARCOTE_BINARY", originalBinary);
            Directory.SetCurrentDirectory(oldCwd);
        }
    }

    [Test]
    public async Task ExecutablePathAsyncShouldReturnCanonicalEnvironmentBinary()
    {
        using var tmpDir = new TempDirectory();
        var originalBinary = Environment.GetEnvironmentVariable("CLEARCOTE_BINARY");
        var executablePath = Path.Combine(tmpDir.Path, OperatingSystem.IsWindows() ? "chrome.exe" : "chrome");
        try
        {
            File.WriteAllText(executablePath, string.Empty);
            Environment.SetEnvironmentVariable("CLEARCOTE_BINARY", executablePath);

            var resolved = await Clearcote.ExecutablePathAsync(null, null, quiet: true, autoUpdate: false);

            Assert.AreEqual(Path.GetFullPath(executablePath), resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLEARCOTE_BINARY", originalBinary);
        }
    }
}
