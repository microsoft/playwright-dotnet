using System.IO.Compression;
using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class SecurityHelpersTests
{
    [Test]
    public void ExtractZipToDirectorySafelyShouldExtractNormalEntries()
    {
        using var tmpDir = new TempDirectory();
        var zipPath = Path.Combine(tmpDir.Path, "safe.zip");
        var destination = Path.Combine(tmpDir.Path, "out");

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("nested/file.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("ok");
        }

        SecurityHelpers.ExtractZipToDirectorySafely(zipPath, destination, overwriteFiles: false);

        Assert.AreEqual("ok", File.ReadAllText(Path.Combine(destination, "nested", "file.txt")));
    }

    [Test]
    public void ExtractZipToDirectorySafelyShouldRejectTraversalEntries()
    {
        using var tmpDir = new TempDirectory();
        var zipPath = Path.Combine(tmpDir.Path, "unsafe.zip");
        var destination = Path.Combine(tmpDir.Path, "out");

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("../escape.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("bad");
        }

        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ExtractZipToDirectorySafely(zipPath, destination, overwriteFiles: false));
        Assert.False(File.Exists(Path.Combine(tmpDir.Path, "escape.txt")));
    }

    [Test]
    public void ExtractZipToDirectorySafelyShouldValidateAllEntriesBeforeWritingFiles()
    {
        using var tmpDir = new TempDirectory();
        var zipPath = Path.Combine(tmpDir.Path, "unsafe-late.zip");
        var destination = Path.Combine(tmpDir.Path, "out");

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var safeEntry = archive.CreateEntry("nested/file.txt");
            using (var writer = new StreamWriter(safeEntry.Open()))
            {
                writer.Write("should-not-be-written");
            }

            var unsafeEntry = archive.CreateEntry("../escape.txt");
            using var unsafeWriter = new StreamWriter(unsafeEntry.Open());
            unsafeWriter.Write("bad");
        }

        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ExtractZipToDirectorySafely(zipPath, destination, overwriteFiles: false));
        Assert.False(File.Exists(Path.Combine(destination, "nested", "file.txt")));
        Assert.False(File.Exists(Path.Combine(tmpDir.Path, "escape.txt")));
    }

    [Test]
    public void ValidateArchiveEntryNameShouldRejectUnsafePaths()
    {
        Assert.DoesNotThrow(() => SecurityHelpers.ValidateArchiveEntryName("nested/file.txt", "test"));
        Assert.DoesNotThrow(() => SecurityHelpers.ValidateArchiveEntryName("nested/", "test"));

        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateArchiveEntryName("../escape.txt", "test"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateArchiveEntryName("nested/../../escape.txt", "test"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateArchiveEntryName("/absolute/file.txt", "test"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateArchiveEntryName("C:/absolute/file.txt", "test"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateArchiveEntryName("nested//file.txt", "test"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateArchiveEntryName("nested/file:stream", "test"));
    }

    [Test]
    public void ValidateTarVerboseEntryTypeShouldRejectLinksAndSpecialFiles()
    {
        Assert.DoesNotThrow(() => SecurityHelpers.ValidateTarVerboseEntryType("-rw-r--r-- user/group 2 2026-07-10 00:00 root/file.txt", "tar"));
        Assert.DoesNotThrow(() => SecurityHelpers.ValidateTarVerboseEntryType("drwxr-xr-x user/group 0 2026-07-10 00:00 root/", "tar"));

        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateTarVerboseEntryType("lrwxrwxrwx user/group 0 2026-07-10 00:00 root/link -> /tmp", "tar"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateTarVerboseEntryType("hrw-r--r-- user/group 0 2026-07-10 00:00 root/hardlink link to root/file.txt", "tar"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateTarVerboseEntryType("crw-r--r-- user/group 0 2026-07-10 00:00 root/device", "tar"));
        Assert.Throws<InvalidDataException>(() => SecurityHelpers.ValidateTarVerboseEntryType("prw-r--r-- user/group 0 2026-07-10 00:00 root/fifo", "tar"));
    }

    [Test]
    public void ValidatePathSegmentShouldRejectTraversal()
    {
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidatePathSegment("../current", "test"));
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidatePathSegment("a/b", "test"));
        Assert.AreEqual("149.0.0.0", SecurityHelpers.ValidatePathSegment("149.0.0.0", "test"));
    }

    [Test]
    public void ResolveAndValidatePathShouldRejectInvalidCharacters()
    {
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ResolveAndValidatePath("bad\0path", "test"));
        Assert.AreEqual(Path.GetFullPath("state.json"), SecurityHelpers.ResolveAndValidatePath("state.json", "test"));
    }

    [Test]
    public void ResolveToolPathFromSearchOutputShouldUseFirstExistingPath()
    {
        using var tmpDir = new TempDirectory();
        var toolPath = Path.Combine(tmpDir.Path, "tool");
        File.WriteAllText(toolPath, string.Empty);

        var output = Path.Combine(tmpDir.Path, "missing") + Environment.NewLine + toolPath + Environment.NewLine;

        Assert.AreEqual(Path.GetFullPath(toolPath), SecurityHelpers.ResolveToolPathFromSearchOutput(output));
    }

    [Test]
    public void GetAbsoluteToolPathShouldRejectUnsafeToolName()
    {
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.GetAbsoluteToolPath("../tool"));
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.GetAbsoluteToolPath("nested/tool"));
    }

    [Test]
    public void ValidateProxyServerShouldNormalizeShortProxyAndRejectUnsafeShapes()
    {
        Assert.AreEqual("http://proxy.test:3128", SecurityHelpers.ValidateProxyServer("proxy.test:3128"));
        Assert.AreEqual("socks5://proxy.test:1080", SecurityHelpers.ValidateProxyServer("socks5://proxy.test:1080"));

        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidateProxyServer("http://user:pass@proxy.test:3128"));
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidateProxyServer("http://proxy.test:3128/path"));
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidateProxyServer("http://proxy.test:3128?x=1"));
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidateProxyServer("http://proxy.test:3128#fragment"));
        Assert.Throws<PlaywrightException>(() => SecurityHelpers.ValidateProxyServer("http://proxy.test:3128\n--flag"));
    }
}
