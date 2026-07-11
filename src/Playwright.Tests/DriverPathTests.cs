using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class DriverPathTests
{
    [Test]
    public void GetExecutablePathShouldRejectRelativeNodeOverride()
    {
        using var tmpDir = new TempDirectory();
        var originalDriverSearchPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        var originalNodePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH");
        var oldCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);
            File.WriteAllText(Path.Combine(tmpDir.Path, OperatingSystem.IsWindows() ? "node.exe" : "node"), string.Empty);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", tmpDir.Path);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", OperatingSystem.IsWindows() ? "node.exe" : "node");

            var exception = Assert.Throws<PlaywrightException>(() => Driver.GetExecutablePath());
            StringAssert.Contains("PLAYWRIGHT_NODEJS_PATH must be a fully-qualified path", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", originalDriverSearchPath);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", originalNodePath);
            Directory.SetCurrentDirectory(oldCwd);
        }
    }

    [Test]
    public void GetExecutablePathShouldValidateDriverEntrypointWhenDriverSearchPathIsOverridden()
    {
        using var tmpDir = new TempDirectory();
        var originalDriverSearchPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        var originalNodePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH");
        try
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", tmpDir.Path);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", null);
            var nodePath = Path.Combine(tmpDir.Path, ".playwright", "node", PlatformId(), OperatingSystem.IsWindows() ? "node.exe" : "node");
            Directory.CreateDirectory(Path.GetDirectoryName(nodePath)!);
            File.WriteAllText(nodePath, string.Empty);

            var exception = Assert.Throws<PlaywrightException>(() => Driver.GetExecutablePath());
            StringAssert.Contains("Couldn't find Playwright driver entrypoint", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", originalDriverSearchPath);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH", originalNodePath);
        }
    }

    private static string PlatformId()
    {
        if (OperatingSystem.IsWindows())
        {
            return "win32_x64";
        }

        if (OperatingSystem.IsMacOS())
        {
            return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64
                ? "darwin-arm64"
                : "darwin-x64";
        }

        return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64
            ? "linux-arm64"
            : "linux-x64";
    }
}
