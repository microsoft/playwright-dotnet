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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/downloads-path.spec.ts.</summary>
public class DownloadsPathTests : UpstreamBrowserTest
{
    public class DownloadsPath : UpstreamBrowserTest
    {
        [SetUp]
        public void Skip() => Info.Skip(Mode != "default", "download.path() is not available in remote mode");

        [SetUp]
        public void BeforeEach()
        {
            Server.SetRoute("/download", (req, res) =>
            {
                res.SetHeader("Content-Type", "application/octet-stream");
                res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
                res.End("Hello world");
            });
        }

        [UpstreamTest("DO-039 should keep downloadsPath folder")]
        public async Task ShouldKeepDownloadsPathFolder()
        {
            var downloadsBrowser = await BrowserType.LaunchAsync(new(LaunchOptions()) { DownloadsPath = Info.OutputPath("") });
            var page = await downloadsBrowser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/download"));
            Assert.That(download.SuggestedFilename, Is.EqualTo("file.txt"));
            await Catch(() => download.PathAsync());
            await page.CloseAsync();
            await downloadsBrowser.CloseAsync();
            Assert.That(Directory.Exists(Info.OutputPath("")), Is.True);
        }

        [UpstreamTest("DO-040 should delete downloads when context closes")]
        public async Task ShouldDeleteDownloadsWhenContextCloses()
        {
            var downloadsBrowser = await BrowserType.LaunchAsync(new(LaunchOptions()) { DownloadsPath = Info.OutputPath("") });
            var page = await downloadsBrowser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            await page.CloseAsync();
            Assert.That(File.Exists(path), Is.False);
            await downloadsBrowser.CloseAsync();
        }

        [UpstreamTest("DO-041 should report downloads in downloadsPath folder")]
        public async Task ShouldReportDownloadsInDownloadsPathFolder()
        {
            var downloadsBrowser = await BrowserType.LaunchAsync(new(LaunchOptions()) { DownloadsPath = Info.OutputPath("") });
            var page = await downloadsBrowser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(path.StartsWith(Info.OutputPath(""), StringComparison.Ordinal), Is.True);
            await page.CloseAsync();
            await downloadsBrowser.CloseAsync();
        }

        [UpstreamTest("DO-042 should report downloads in downloadsPath folder with a relative path")]
        public async Task ShouldReportDownloadsInDownloadsPathFolderWithARelativePath()
        {
            var downloadsBrowser = await BrowserType.LaunchAsync(new(LaunchOptions()) { DownloadsPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Info.OutputPath("")) });
            var page = await downloadsBrowser.NewPageAsync(NewPageOptions());
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var downloadPath = await download.PathAsync();
            Assert.That(downloadPath.StartsWith(Info.OutputPath(""), StringComparison.Ordinal), Is.True);
            await page.CloseAsync();
            await downloadsBrowser.CloseAsync();
        }

        [UpstreamTest("DO-043 should accept downloads in persistent context")]
        public async Task ShouldAcceptDownloadsInPersistentContext()
        {
            var (context, page) = await LaunchPersistent(new() { DownloadsPath = Info.OutputPath("") });
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            Assert.That(download.Url, Is.EqualTo($"{Server.Prefix}/download"));
            Assert.That(download.SuggestedFilename, Is.EqualTo("file.txt"));
            var path = await download.PathAsync();
            Assert.That(path.StartsWith(Info.OutputPath(""), StringComparison.Ordinal), Is.True);
            await context.CloseAsync();
        }

        [UpstreamTest("DO-044 should delete downloads when persistent context closes")]
        public async Task ShouldDeleteDownloadsWhenPersistentContextCloses()
        {
            var (context, page) = await LaunchPersistent(new() { DownloadsPath = Info.OutputPath("") });
            await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
            var download = await PromiseAll(
                page.WaitForDownloadAsync(),
                page.ClickAsync("a"));
            var path = await download.PathAsync();
            Assert.That(File.Exists(path), Is.True);
            await context.CloseAsync();
            Assert.That(File.Exists(path), Is.False);
        }
    }
}
