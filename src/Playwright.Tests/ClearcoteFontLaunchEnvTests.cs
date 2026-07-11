using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ClearcoteFontLaunchEnvTests
{
    [Test]
    public void FontLaunchEnvShouldWriteGeneratedConfigOutsideBrowserDirectory()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Ignore("Clearcote fontconfig launch env is Linux-specific.");
        }

        using var tmpDir = new TempDirectory();
        var browserDir = Path.Combine(tmpDir.Path, "browser");
        var fontsDir = Path.Combine(browserDir, "fonts");
        Directory.CreateDirectory(fontsDir);
        var executablePath = Path.Combine(browserDir, "chrome");
        File.WriteAllText(executablePath, string.Empty);
        File.WriteAllText(
            Path.Combine(fontsDir, "fonts.conf.template"),
            "<fontconfig><dir>@FONTS_DIR@</dir><cachedir>@CACHE_DIR@</cachedir></fontconfig>");

        var env = Clearcote.FontLaunchEnv(executablePath, null);

        Assert.NotNull(env);
        Assert.True(env!.TryGetValue("FONTCONFIG_FILE", out var generatedConfig));
        Assert.NotNull(generatedConfig);
        Assert.True(File.Exists(generatedConfig));
        Assert.False(File.Exists(Path.Combine(fontsDir, "fonts.generated.conf")));
        StringAssert.StartsWith(Path.Combine(Path.GetTempPath(), "cc-fc-cache"), generatedConfig);
        var contents = File.ReadAllText(generatedConfig!);
        StringAssert.Contains(fontsDir, contents);
        StringAssert.Contains(Path.Combine(Path.GetTempPath(), "cc-fc-cache"), contents);
    }
}
