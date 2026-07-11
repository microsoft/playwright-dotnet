using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using Playwright.Tooling;

namespace Microsoft.Playwright.Tests;

public class DriverDownloaderTests
{
    [Test]
    public async Task ExtractPlaywrightPackageAsyncShouldNotPublishPartialPackageOnUnsafeLateEntry()
    {
        using var tmpDir = new TempDirectory();
        var driversDirectory = Path.Combine(tmpDir.Path, ".drivers");
        await using var archive = CreatePlaywrightPackageArchive(
            ("package/safe.txt", "safe"),
            ("package/../escape.txt", "bad"));

        Assert.ThrowsAsync<InvalidDataException>(() => DriverDownloader.ExtractPlaywrightPackageAsync(archive, driversDirectory));
        Assert.False(Directory.Exists(Path.Combine(driversDirectory, "package")));
        Assert.False(File.Exists(Path.Combine(tmpDir.Path, "escape.txt")));
    }

    [Test]
    public async Task ExtractPlaywrightPackageAsyncShouldPublishPackageAfterFullValidation()
    {
        using var tmpDir = new TempDirectory();
        var driversDirectory = Path.Combine(tmpDir.Path, ".drivers");
        await using var archive = CreatePlaywrightPackageArchive(
            ("package/lib/cli.js", "console.log('ok');"),
            ("package/package.json", "{}"));

        await DriverDownloader.ExtractPlaywrightPackageAsync(archive, driversDirectory);

        Assert.AreEqual("console.log('ok');", File.ReadAllText(Path.Combine(driversDirectory, "package", "lib", "cli.js")));
        Assert.AreEqual("{}", File.ReadAllText(Path.Combine(driversDirectory, "package", "package.json")));
    }

    private static MemoryStream CreatePlaywrightPackageArchive(params (string Name, string Content)[] entries)
    {
        var archive = new MemoryStream();
        using (var gzip = new GZipStream(archive, CompressionLevel.SmallestSize, leaveOpen: true))
        using (var writer = new TarWriter(gzip, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var data = new MemoryStream(Encoding.UTF8.GetBytes(content));
                var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
                {
                    DataStream = data,
                    Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
                };
                writer.WriteEntry(entry);
            }
        }

        archive.Position = 0;
        return archive;
    }
}
