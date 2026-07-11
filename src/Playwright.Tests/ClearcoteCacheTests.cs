using Microsoft.Playwright.Helpers;
using NUnit.Framework;
using System.Security.Cryptography;

namespace Microsoft.Playwright.Tests;

public class ClearcoteCacheTests
{
    [Test]
    [NonParallelizable]
    public void ResolveCacheRootShouldCanonicalizeExplicitDirectory()
    {
        using var tmpDir = new TempDirectory();
        var previousCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);

            var resolved = Clearcote.ResolveCacheRoot("cache");

            Assert.AreEqual(Path.Combine(tmpDir.Path, "cache"), resolved);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCurrentDirectory);
        }
    }

    [Test]
    [NonParallelizable]
    public void ResolveCacheRootShouldRejectRelativeEnvironmentDirectory()
    {
        using var tmpDir = new TempDirectory();
        var previousCurrentDirectory = Directory.GetCurrentDirectory();
        var originalCache = Environment.GetEnvironmentVariable("CLEARCOTE_CACHE");
        try
        {
            Directory.SetCurrentDirectory(tmpDir.Path);
            Environment.SetEnvironmentVariable("CLEARCOTE_CACHE", "cache");

            var exception = Assert.Throws<PlaywrightException>(() => Clearcote.ResolveCacheRoot(null));

            StringAssert.Contains("CLEARCOTE_CACHE must be a fully-qualified path", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLEARCOTE_CACHE", originalCache);
            Directory.SetCurrentDirectory(previousCurrentDirectory);
        }
    }

    [Test]
    [NonParallelizable]
    public void ResolveCacheRootShouldReturnCanonicalEnvironmentDirectory()
    {
        using var tmpDir = new TempDirectory();
        var originalCache = Environment.GetEnvironmentVariable("CLEARCOTE_CACHE");
        try
        {
            Environment.SetEnvironmentVariable("CLEARCOTE_CACHE", tmpDir.Path);

            var resolved = Clearcote.ResolveCacheRoot(null);

            Assert.AreEqual(Path.GetFullPath(tmpDir.Path), resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLEARCOTE_CACHE", originalCache);
        }
    }

    [Test]
    public void PublishVerifiedBrowserDirectoryShouldReplaceBrowserAfterVerification()
    {
        using var tmpDir = new TempDirectory();
        var browserDir = Path.Combine(tmpDir.Path, "browser");
        var stagingDir = Path.Combine(tmpDir.Path, "browser-staging");
        var verifiedPath = Path.Combine(tmpDir.Path, ".verified");

        Directory.CreateDirectory(browserDir);
        File.WriteAllText(Path.Combine(browserDir, "chrome"), "old");
        Directory.CreateDirectory(stagingDir);
        File.WriteAllText(Path.Combine(stagingDir, "chrome"), "new");

        Clearcote.PublishVerifiedBrowserDirectory(stagingDir, browserDir, verifiedPath, "abc123");

        Assert.False(Directory.Exists(stagingDir));
        Assert.AreEqual("new", File.ReadAllText(Path.Combine(browserDir, "chrome")));
        Assert.AreEqual("abc123" + Environment.NewLine, File.ReadAllText(verifiedPath));
    }

    [Test]
    public void PublishVerifiedBrowserDirectoryShouldRestoreExistingBrowserWhenVerificationStampFails()
    {
        using var tmpDir = new TempDirectory();
        var browserDir = Path.Combine(tmpDir.Path, "browser");
        var stagingDir = Path.Combine(tmpDir.Path, "browser-staging");
        var invalidVerifiedParent = Path.Combine(tmpDir.Path, "not-a-directory");
        var verifiedPath = Path.Combine(invalidVerifiedParent, ".verified");

        Directory.CreateDirectory(browserDir);
        File.WriteAllText(Path.Combine(browserDir, "chrome"), "old");
        Directory.CreateDirectory(stagingDir);
        File.WriteAllText(Path.Combine(stagingDir, "chrome"), "new");
        File.WriteAllText(invalidVerifiedParent, "file");

        Assert.Throws<DirectoryNotFoundException>(() => Clearcote.PublishVerifiedBrowserDirectory(stagingDir, browserDir, verifiedPath, "abc123"));

        Assert.AreEqual("old", File.ReadAllText(Path.Combine(browserDir, "chrome")));
        Assert.False(Directory.Exists(stagingDir));
    }

    [Test]
    public async Task TryGetVerifiedCachedExecutableAsyncShouldRejectStaleVerificationStamp()
    {
        using var tmpDir = new TempDirectory();
        var browserDir = Path.Combine(tmpDir.Path, "browser");
        var verifiedPath = Path.Combine(tmpDir.Path, ".verified");
        Directory.CreateDirectory(browserDir);
        File.WriteAllText(Path.Combine(browserDir, "chrome"), "expected");
        File.WriteAllText(verifiedPath, "stale" + Environment.NewLine);

        var resolved = await Clearcote.TryGetVerifiedCachedExecutableAsync(
            browserDir,
            verifiedPath,
            "chrome",
            expectedArchiveSha256: "archive-sha",
            expectedExecutableSha256: string.Empty,
            quiet: true);

        Assert.Null(resolved);
    }

    [Test]
    public async Task TryGetVerifiedCachedExecutableAsyncShouldRejectMismatchedExecutableHash()
    {
        using var tmpDir = new TempDirectory();
        var browserDir = Path.Combine(tmpDir.Path, "browser");
        var verifiedPath = Path.Combine(tmpDir.Path, ".verified");
        Directory.CreateDirectory(browserDir);
        File.WriteAllText(Path.Combine(browserDir, "chrome"), "tampered");
        File.WriteAllText(verifiedPath, "archive-sha" + Environment.NewLine);

        var resolved = await Clearcote.TryGetVerifiedCachedExecutableAsync(
            browserDir,
            verifiedPath,
            "chrome",
            expectedArchiveSha256: "archive-sha",
            expectedExecutableSha256: new string('0', 64),
            quiet: true);

        Assert.Null(resolved);
    }

    [Test]
    public async Task TryGetVerifiedCachedExecutableAsyncShouldReturnCacheWhenStampAndExecutableHashMatch()
    {
        using var tmpDir = new TempDirectory();
        var browserDir = Path.Combine(tmpDir.Path, "browser");
        var verifiedPath = Path.Combine(tmpDir.Path, ".verified");
        var executablePath = Path.Combine(browserDir, "chrome");
        Directory.CreateDirectory(browserDir);
        File.WriteAllText(executablePath, "expected");
        File.WriteAllText(verifiedPath, "archive-sha" + Environment.NewLine);

        var resolved = await Clearcote.TryGetVerifiedCachedExecutableAsync(
            browserDir,
            verifiedPath,
            "chrome",
            expectedArchiveSha256: "archive-sha",
            expectedExecutableSha256: Sha256(executablePath),
            quiet: true);

        Assert.AreEqual(executablePath, resolved);
    }

    [Test]
    public void PublishDownloadedExecutableShouldPublishThroughCanonicalDestinationPath()
    {
        using var tmpDir = new TempDirectory();
        var sourcePath = Path.Combine(tmpDir.Path, "source", "chrome");
        var destinationPath = Path.Combine(tmpDir.Path, "out", "chrome");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, "new");

        var resolved = Clearcote.PublishDownloadedExecutable(sourcePath, destinationPath);

        Assert.AreEqual(Path.GetFullPath(destinationPath), resolved);
        Assert.AreEqual("new", File.ReadAllText(destinationPath));
    }

    [Test]
    public void PublishDownloadedExecutableShouldRemoveStagingFileWhenReplaceFails()
    {
        using var tmpDir = new TempDirectory();
        var sourcePath = Path.Combine(tmpDir.Path, "source", "chrome");
        var destinationPath = Path.Combine(tmpDir.Path, "chrome");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        Directory.CreateDirectory(destinationPath);
        File.WriteAllText(sourcePath, "new");

        Assert.Throws<IOException>(() => Clearcote.PublishDownloadedExecutable(sourcePath, destinationPath));

        Assert.True(Directory.Exists(destinationPath));
        Assert.False(Directory.EnumerateFiles(tmpDir.Path, ".chrome.download-*").Any());
    }

    private static string Sha256(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }
}
