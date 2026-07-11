using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class ClearcoteWidevineTests
{
    [Test]
    public void CopyDirectorySafelyShouldCopyNestedFiles()
    {
        using var tmpDir = new TempDirectory();
        var source = Path.Combine(tmpDir.Path, "source");
        var target = Path.Combine(tmpDir.Path, "target");
        Directory.CreateDirectory(Path.Combine(source, "nested"));
        File.WriteAllText(Path.Combine(source, "nested", "manifest.json"), "ok");

        Clearcote.CopyDirectorySafely(source, target);

        Assert.AreEqual("ok", File.ReadAllText(Path.Combine(target, "nested", "manifest.json")));
    }

    [Test]
    public void CopyDirectorySafelyShouldNotOverwriteExistingFiles()
    {
        using var tmpDir = new TempDirectory();
        var source = Path.Combine(tmpDir.Path, "source");
        var target = Path.Combine(tmpDir.Path, "target");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(source, "manifest.json"), "new");
        File.WriteAllText(Path.Combine(target, "manifest.json"), "old");

        Assert.Throws<IOException>(() => Clearcote.CopyDirectorySafely(source, target));

        Assert.AreEqual("old", File.ReadAllText(Path.Combine(target, "manifest.json")));
    }

    [Test]
    public void CopyDirectorySafelyShouldRejectSourceReparsePoints()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Ignore("Symlink regression coverage is Linux-specific.");
        }

        using var tmpDir = new TempDirectory();
        var source = Path.Combine(tmpDir.Path, "source");
        var target = Path.Combine(tmpDir.Path, "target");
        Directory.CreateDirectory(source);
        var realFile = Path.Combine(source, "real.txt");
        var linkFile = Path.Combine(source, "link.txt");
        File.WriteAllText(realFile, "ok");

        try
        {
            File.CreateSymbolicLink(linkFile, realFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Assert.Ignore("Could not create a symlink in this environment: " + ex.Message);
        }

        var exception = Assert.Throws<PlaywrightException>(() => Clearcote.CopyDirectorySafely(source, target));

        StringAssert.Contains("Refusing to copy reparse point", exception.Message);
    }
}
