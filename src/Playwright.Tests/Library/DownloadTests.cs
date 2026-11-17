/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/download.spec.ts.</summary>
public class DownloadTests : UpstreamBrowserTest
{
    public class DownloadEvent : UpstreamBrowserTest
    {
        [SetUp]
        public void Skip() => Info.Skip(Mode != "default", "download.path() is not available in remote mode");

        [SetUp]
        public void BeforeEach()
        {
            Server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment");
                res.End("Hello world");
            });
            Server.SetRoute("/downloadWithFilename", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
                res.End("Hello world");
            });
            Server.SetRoute("/downloadWithDelay", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
                // Chromium requires a large enough payload to trigger the download event soon enough
                res.Write(new string('a', 4096));
                res.Write("foo");
                res.Uncork();
            });
            Server.SetRoute("/downloadWithCOOP", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment");
                res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
                res.End("Hello world");
            });
        }

        [UpstreamTest("DO-001 should report download when navigation turns into download @smoke")]
        public async Task ShouldReportDownloadWhenNavigationTurnsIntoDownload()
        {
            Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 140, "old chromium throws net::ERR_ABORTED, depends on https://chromium-review.googlesource.com/c/chromium/src/+/6696011");
            var page = await Browser.NewPageAsync(NewPageOptions());
            var (download, responseOrError) = await PromiseAll(
                page.WaitForDownloadAsync(),
                Catch(() => page.GotoAsync(Server.Prefix + "/download")));
            Assert.That(download.Page, Is.SameAs(page));
            Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/download"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));

            Assert.That(responseOrError is Exception, Is.True);
            Assert.That(responseOrError.Message, Does.Contain("Download is starting"));

            if (BrowserName != "firefox" || IsBidi)
            {
                Assert.That(page.Url, Is.EqualTo("about:blank"));
            }
            await page.CloseAsync();
        }

        [UpstreamTest("DO-002 should work with Cross-Origin-Opener-Policy")]
        public async Task ShouldWorkWithCrossOriginOpenerPolicy()
        {
            Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 140, "old chromium throws net::ERR_ABORTED, depends on https://chromium-review.googlesource.com/c/chromium/src/+/6696011");
            var page = await Browser.NewPageAsync(NewPageOptions());
            var (download, responseOrError) = await PromiseAll(
                page.WaitForDownloadAsync(),
                Catch(() => page.GotoAsync(Server.Prefix + "/downloadWithCOOP")));
            Assert.That(download.Page, Is.SameAs(page));
            Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/downloadWithCOOP"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            Assert.That(responseOrError is Exception, Is.True);
            Assert.That(responseOrError.Message, Does.Contain("Download is starting"));
            if (BrowserName != "firefox" || IsBidi)
            {
                Assert.That(page.Url, Is.EqualTo("about:blank"));
            }
            await page.CloseAsync();
        }

        [UpstreamTest("DO-003 should report downloads with acceptDownloads: false")]
        public async Task ShouldReportDownloadsWithAcceptDownloadsFalse()
        {
            var page = await Browser.NewPageAsync(new(NewPageOptions()) { AcceptDownloads = false });
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/downloadWithFilename\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Exception error = null;
            Assert.That(download.Page, Is.SameAs(page));
            Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/downloadWithFilename"));
            Assert.That(download.SuggestedFilename, Is.EqualTo("file.txt"));
            error = await Catch(() => download.PathAsync());
            Assert.That(await download.FailureAsync(), Does.Contain("acceptDownloads"));
            Assert.That(error.Message, Does.Contain("acceptDownloads: true"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-004 should report downloads with acceptDownloads: true")]
        public async Task ShouldReportDownloadsWithAcceptDownloadsTrue()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-005 should report proper download url when download is from download attribute")]
        public async Task ShouldReportProperDownloadUrlWhenDownloadIsFromDownloadAttribute()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.Prefix + "/empty.html");
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/chromium-linux.zip\" download=\"foo.zip\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/chromium-linux.zip"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-006 should report downloads for download attribute")]
        public async Task ShouldReportDownloadsForDownloadAttribute()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.Prefix + "/empty.html");
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/chromium-linux.zip\" download=\"foo.zip\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Assert.That(download.SuggestedFilename, Is.EqualTo("foo.zip"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            await page.CloseAsync();
        }

        [UpstreamTest("DO-007 should save to user-specified path without updating original path")]
        public async Task ShouldSaveToUserSpecifiedPathWithoutUpdatingOriginalPath()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var userPath = Info.OutputPath("download.txt");
            await download.SaveAsAsync(userPath);
            Assert.That(File.Exists(userPath), Is.True);
            Assert.That(File.ReadAllText(userPath), Is.EqualTo("Hello world"));

            var originalPath = await download.PathAsync();
            Assert.That(File.Exists(originalPath), Is.True);
            Assert.That(File.ReadAllText(originalPath), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-008 should save to two different paths with multiple saveAs calls")]
        public async Task ShouldSaveToTwoDifferentPathsWithMultipleSaveAsCalls()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var userPath = Info.OutputPath("download.txt");
            await download.SaveAsAsync(userPath);
            Assert.That(File.Exists(userPath), Is.True);
            Assert.That(File.ReadAllText(userPath), Is.EqualTo("Hello world"));

            var anotherUserPath = Info.OutputPath("download (2).txt");
            await download.SaveAsAsync(anotherUserPath);
            Assert.That(File.Exists(anotherUserPath), Is.True);
            Assert.That(File.ReadAllText(anotherUserPath), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-009 should save to overwritten filepath")]
        public async Task ShouldSaveToOverwrittenFilepath()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var dir = Info.OutputPath("downloads");
            var userPath = Path.Combine(dir, "download.txt");
            await download.SaveAsAsync(userPath);
            Assert.That(Directory.GetFileSystemEntries(dir).Length, Is.EqualTo(1));
            await download.SaveAsAsync(userPath);
            Assert.That(Directory.GetFileSystemEntries(dir).Length, Is.EqualTo(1));
            Assert.That(File.Exists(userPath), Is.True);
            Assert.That(File.ReadAllText(userPath), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-010 should create subdirectories when saving to non-existent user-specified path")]
        public async Task ShouldCreateSubdirectoriesWhenSavingToNonExistentUserSpecifiedPath()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var nestedPath = Info.OutputPath(Path.Combine("these", "are", "directories", "download.txt"));
            await download.SaveAsAsync(nestedPath);
            Assert.That(File.Exists(nestedPath), Is.True);
            Assert.That(File.ReadAllText(nestedPath), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-011 should error when saving with downloads disabled")]
        public async Task ShouldErrorWhenSavingWithDownloadsDisabled()
        {
            var page = await Browser.NewPageAsync(new(NewPageOptions()) { AcceptDownloads = false });
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var userPath = Info.OutputPath("download.txt");
            var message = (await Catch(() => download.SaveAsAsync(userPath))).Message;
            Assert.That(message, Does.Contain("Pass { acceptDownloads: true } when you are creating your browser context"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-012 should error when saving after deletion")]
        public async Task ShouldErrorWhenSavingAfterDeletion()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var userPath = Info.OutputPath("download.txt");
            await download.DeleteAsync();
            var message = (await Catch(() => download.SaveAsAsync(userPath))).Message;
            Assert.That(message, Does.Contain("Target page, context or browser has been closed"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-013 should report non-navigation downloads")]
        public async Task ShouldReportNonNavigationDownloads()
        {
            // Mac WebKit embedder does not download in this case, although Safari does.
            Server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.End("Hello world");
            });

            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync($"<a download=\"file.txt\" href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Assert.That(download.SuggestedFilename, Is.EqualTo("file.txt"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-014 should report download path within page.on('download', …) handler for Files")]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForFiles()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            var onDownloadPath = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            page.Download += (_, dl) =>
            {
                async Task Then() => onDownloadPath.TrySetResult(await dl.PathAsync());
                IgnoreErrors(Then());
            };
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            await page.ClickAsync("a");
            var path = await onDownloadPath.Task;
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-015 should report download path within page.on('download', …) handler for Blobs")]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForBlobs()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            var onDownloadPath = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            page.Download += (_, dl) =>
            {
                async Task Then() => onDownloadPath.TrySetResult(await dl.PathAsync());
                IgnoreErrors(Then());
            };
            await page.GotoAsync(Server.Prefix + "/download-blob.html");
            await page.ClickAsync("a");
            var path = await onDownloadPath.Task;
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-016 should report alt-click downloads")]
        public async Task ShouldReportAltClickDownloads()
        {
            Info.Skip(BrowserName == "firefox", "Firefox does not download on alt-click.");

            Server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.End("Hello world");
            });

            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a", new() { Modifiers = new[] { KeyboardModifier.Alt } }));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-017 should report new window downloads")]
        public async Task ShouldReportNewWindowDownloads()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a target=_blank href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            await page.CloseAsync();
        }

        [UpstreamTest("DO-018 should delete file")]
        public async Task ShouldDeleteFile()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            await download.DeleteAsync();
            Assert.That(File.Exists(path), Is.False);
            await page.CloseAsync();
        }

        [UpstreamTest("DO-019 should expose stream")]
        public async Task ShouldExposeStream()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var stream = await download.CreateReadStreamAsync();
            var content = "";
            // .NET: a Stream is read to its end; a Node.js stream raises 'data' and 'end'.
            content += await new StreamReader(stream).ReadToEndAsync();
            Assert.That(content, Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-020 should delete downloads on context destruction")]
        public async Task ShouldDeleteDownloadsOnContextDestruction()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download1 = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var download2 = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path1 = await download1.PathAsync();
            var path2 = await download2.PathAsync();
            Assert.That(File.Exists(path1), Is.True);
            Assert.That(File.Exists(path2), Is.True);
            await page.Context.CloseAsync();
            Assert.That(File.Exists(path1), Is.False);
            Assert.That(File.Exists(path2), Is.False);
        }

        [UpstreamTest("DO-021 should delete downloads on browser gone")]
        public async Task ShouldDeleteDownloadsOnBrowserGone()
        {
            var browser = await BrowserType.LaunchAsync(new(LaunchOptions()) { ArtifactsDir = null });
            var page = await browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download1 = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var download2 = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path1 = await download1.PathAsync();
            var path2 = await download2.PathAsync();
            Assert.That(File.Exists(path1), Is.True);
            Assert.That(File.Exists(path2), Is.True);
            await browser.CloseAsync();
            Assert.That(File.Exists(path1), Is.False);
            Assert.That(File.Exists(path2), Is.False);
            // .NET: path.join() resolves '..' in the path, Path.Combine() does not.
            Assert.That(Directory.Exists(Path.GetFullPath(Path.Combine(path1, ".."))), Is.False);
        }

        [UpstreamTest("DO-022 should save downloads to artifactsDir")]
        public async Task ShouldSaveDownloadsToArtifactsDir()
        {
            Info.Skip(IsFrozenWebkit, "needs recent webkit");
            var artifactsDir = Info.OutputPath("artifacts");
            var browser = await BrowserType.LaunchAsync(new(LaunchOptions()) { ArtifactsDir = artifactsDir });
            var page = await browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var downloadPath = await download.PathAsync();
            Assert.That(downloadPath.StartsWith(artifactsDir, StringComparison.Ordinal), Is.True);
            Assert.That(File.Exists(downloadPath), Is.True);
            await browser.CloseAsync();
            // User-provided artifactsDir should not be cleaned up.
            Assert.That(Directory.Exists(artifactsDir), Is.True);
        }

        [UpstreamTest("DO-023 should close the context without awaiting the failed download")]
        [LocalFixme("dotnet: the message is 'canceled', without the 'download.path: ' prefix of the call")]
        public async Task ShouldCloseTheContextWithoutAwaitingTheFailedDownload()
        {
            Info.Skip(BrowserName != "chromium", "Only Chromium downloads on alt-click");

            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync($"<a href=\"{HttpsServer.Prefix}/downloadWithFilename\" download=\"file.txt\">click me</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                // Use alt-click to force the download. Otherwise browsers might try to navigate first,
                // probably because of http -> https link.
                page.ClickAsync("a", new() { Modifiers = new[] { KeyboardModifier.Alt } }));
            var (downloadError, saveError) = await PromiseAll(
                Catch(() => download.PathAsync()),
                Catch(() => download.SaveAsAsync(Info.OutputPath("download.txt"))),
                page.Context.CloseAsync());
            Assert.That(downloadError.Message, Is.EqualTo("download.path: canceled"));
            Assert.That(
                new[]
                {
                    "download.saveAs: File not found on disk. Check download.failure() for details.",
                    "download.saveAs: canceled",
                },
                Does.Contain(saveError.Message));
        }

        [UpstreamTest("DO-024 should close the context without awaiting the download")]
        [LocalFixme("dotnet: the message is 'canceled', without the 'download.path: ' prefix of the call (only in chromium and firefox); dotnet: the message is 'Target page, context or browser has been closed', without the 'download.path: ' prefix of the call (only in webkit)")]
        public async Task ShouldCloseTheContextWithoutAwaitingTheDownload()
        {
            Info.Skip(BrowserName == "webkit" && (Platform == "linux" || Channel == "webkit-wsl"), "WebKit on linux does not convert to the download immediately upon receiving headers");

            Server.SetRoute("/downloadStall", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
                res.WriteHead(200);
                res.FlushHeaders();
                res.Write("Hello world");
            });

            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/downloadStall\" download=\"file.txt\">click me</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var (downloadError, saveError) = await PromiseAll(
                Catch(() => download.PathAsync()),
                Catch(() => download.SaveAsAsync(Info.OutputPath("download.txt"))),
                page.Context.CloseAsync());
            // The exact error message is racy, because sometimes browser is fast enough
            // to cancel the download.
            Assert.That(
                new[]
                {
                    "download.path: canceled",
                    "download.path: " + TargetClosedErrorMessage,
                },
                Does.Contain(downloadError.Message));
            Assert.That(
                new[]
                {
                    "download.saveAs: canceled",
                    "download.saveAs: " + TargetClosedErrorMessage,
                },
                Does.Contain(saveError.Message));
        }

        [UpstreamTest("DO-026 should download large binary.zip")]
        public async Task ShouldDownloadLargeBinaryZip()
        {
            var zipFile = Info.OutputPath("binary.zip");
            var content = RandomNumberGenerator.GetBytes(1 << 20);
            File.WriteAllBytes(zipFile, content);
            Server.SetRoute("/binary.zip", (req, res) => Server.ServeFile(req, res, zipFile));

            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync(Server.Prefix + "/empty.html");
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/binary.zip\" download=\"binary.zip\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var downloadPath = await download.PathAsync();
            var fileContent = File.ReadAllBytes(downloadPath);
            Assert.That(fileContent.Length, Is.EqualTo(content.Length));
            Assert.That(fileContent.SequenceEqual(content), Is.EqualTo(true));

            var stream = await download.CreateReadStreamAsync();
            // .NET: a Stream is read to its end; a Node.js stream raises 'data', 'error' and 'end'.
            var buffs = new MemoryStream();
            await stream.CopyToAsync(buffs);
            var data = buffs.ToArray();
            Assert.That(data.Length, Is.EqualTo(content.Length));
            Assert.That(data.SequenceEqual(content), Is.EqualTo(true));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-027 should be able to cancel pending downloads")]
        public async Task ShouldBeAbleToCancelPendingDownloads()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/downloadWithDelay\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            await download.CancelAsync();
            var failure = await download.FailureAsync();
            Assert.That(failure, Is.EqualTo("canceled"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-028 should not fail explicitly to cancel a download even if that is already finished")]
        public async Task ShouldNotFailExplicitlyToCancelADownloadEvenIfThatIsAlreadyFinished()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await download.CancelAsync();
            var failure = await download.FailureAsync();
            Assert.That(failure, Is.Null);
            await page.CloseAsync();
        }

        [UpstreamTest("DO-029 should report downloads with interception")]
        public async Task ShouldReportDownloadsWithInterception()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            await page.RouteAsync(new Regex(".*"), r => r.ContinueAsync());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }

        [UpstreamTest("DO-030 should emit download event from nested iframes")]
        public async Task ShouldEmitDownloadEventFromNestedIframes()
        {
            var page = await Browser.NewPageAsync(NewPageOptions());
            Server.SetRoute("/1", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End($"<iframe src=\"{Server.Prefix}/2\"></iframe>");
            });
            Server.SetRoute("/2", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End($"<iframe src=\"{Server.Prefix}/3\"></iframe>");
            });
            Server.SetRoute("/3", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End($" <a href=\"{Server.Prefix}/download\">download</a>");
            });
            await page.GotoAsync(Server.Prefix + "/1");

            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.FrameByUrl(Server.Prefix + "/3").ClickAsync("text=download"));
            var userPath = Info.OutputPath("download.txt");
            await download.SaveAsAsync(userPath);
            Assert.That(File.Exists(userPath), Is.True);
            Assert.That(File.ReadAllText(userPath), Is.EqualTo("Hello world"));
            await page.CloseAsync();
        }
    }

    [UpstreamTest("DO-031 should be able to download a PDF file")]
    public async Task ShouldBeAbleToDownloadAPDFFile()
    {
        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync(@"
    <a href=""/empty.pdf"" download>download</a>
  ");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        await AssertDownloadToPDF(download, Asset("empty.pdf"));
        await page.CloseAsync();
    }

    [UpstreamTest("DO-032 should be able to download a inline PDF file via response interception")]
    public async Task ShouldBeAbleToDownloadAInlinePDFFileViaResponseInterception()
    {
        Info.Fixme(BrowserName == "webkit");
        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync(Server.EmptyPage);
        await page.RouteAsync("**/empty.pdf", async route =>
        {
            var response = await page.Context.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Response = response,
                Headers = new Dictionary<string, string>(response.Headers)
                {
                    ["Content-Disposition"] = "attachment",
                },
            });
        });
        await page.SetContentAsync(@"
    <a href=""/empty.pdf"">open</a>
  ");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        await AssertDownloadToPDF(download, Asset("empty.pdf"));
        await page.CloseAsync();
    }

    [UpstreamTest("DO-033 should be able to download a inline PDF file via navigation")]
    public async Task ShouldBeAbleToDownloadAInlinePDFFileViaNavigation()
    {
        Info.Skip(BrowserName == "chromium" && !IsHeadlessShell, "We expect PDF Viewer to open up in headed Chromium");
        Info.Skip(BrowserName == "firefox" && IsBidi, "We expect PDF Viewer to open up in Firefox with Bidi");

        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync(@"
    <a href=""/empty.pdf"">open</a>
  ");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        await AssertDownloadToPDF(download, Asset("empty.pdf"));
        await page.CloseAsync();
    }

    [UpstreamTest("DO-034 should save to user-specified path")]
    public async Task ShouldSaveToUserSpecifiedPath()
    {
        Server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.SetHeader("Content-Disposition", "attachment");
            res.End("Hello world");
        });

        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        var userPath = Info.OutputPath("download.txt");
        await download.SaveAsAsync(userPath);
        Assert.That(File.Exists(userPath), Is.True);
        Assert.That(File.ReadAllText(userPath), Is.EqualTo("Hello world"));
        await page.CloseAsync();
    }

    [UpstreamTest("DO-035 should download even if there is no \"attachment\" value")]
    public async Task ShouldDownloadEvenIfThereIsNoAttachmentValue()
    {
        Server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            // Do not set the "attachment" value.
            res.SetHeader("Content-Disposition", "filename=foo.txt");
            res.End("Hello world");
        });

        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        await page.CloseAsync();
    }

    [UpstreamTest("DO-036 should convert navigation to a resource with unsupported mime type into download")]
    public async Task ShouldConvertNavigationToAResourceWithUnsupportedMimeTypeIntoDownload()
    {
        Server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.End("Hello world");
        });
        var page = await Browser.NewPageAsync(NewPageOptions());
        await PromiseAll(
            page.WaitForDownloadAsync(),
            Catch(() => page.GotoAsync($"{Server.Prefix}/download")));
        await page.CloseAsync();
    }

    [UpstreamTest("DO-037 should download links with data url")]
    public async Task ShouldDownloadLinksWithDataUrl()
    {
        var page = await PageFixture();
        await page.SetContentAsync("<a download=\"SomeFile.txt\" href=\"data:text/plain;charset=utf8;,hello world\">Download!</a>");
        var downloadPromise = page.WaitForDownloadAsync();
        await page.GetByText("Download").ClickAsync();
        var download = await downloadPromise;
        Assert.That(download.SuggestedFilename, Is.EqualTo("SomeFile.txt"));
    }

    [UpstreamTest("DO-038 should download successfully when routing")]
    public async Task ShouldDownloadSuccessfullyWhenRouting()
    {
        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.Context.RouteAsync("**/*", route => route.ContinueAsync());
        await page.GotoAsync(Server.Prefix + "/empty.html");
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/chromium-linux.zip\" download=\"foo.zip\">download</a>");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        Assert.That(download.SuggestedFilename, Is.EqualTo("foo.zip"));
        Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/chromium-linux.zip"));
        Assert.That(await download.FailureAsync(), Is.Null);
        await page.CloseAsync();
    }

    private static async Task AssertDownloadToPDF(IDownload download, string filePath)
    {
        Assert.That(download.SuggestedFilename, Is.EqualTo(Path.GetFileName(filePath)));
        var stream = await download.CreateReadStreamAsync();
        // .NET: a Stream is read to its end; a Node.js stream raises 'data', 'error' and 'end'.
        var buffs = new MemoryStream();
        await stream.CopyToAsync(buffs);
        var data = buffs.ToArray();
        Assert.That(download.Url.EndsWith("/" + Path.GetFileName(filePath), StringComparison.Ordinal), Is.True);
        var expectedPrefix = "%PDF";
        for (var i = 0; i < expectedPrefix.Length; i++)
        {
            Assert.That(data[i], Is.EqualTo((int)expectedPrefix[i]));
        }
        AssertBuffer(data, File.ReadAllBytes(filePath));
    }

    private static void AssertBuffer(byte[] expected, byte[] actual)
    {
        Assert.That(expected.Length, Is.EqualTo(actual.Length));
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.That(expected[i], Is.EqualTo(actual[i]));
        }
    }
}
