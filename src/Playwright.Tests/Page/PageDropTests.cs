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

using System.Text;
using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-drop.spec.ts.</summary>
public class PageDropTests : UpstreamPageTest
{
    [SetUp]
    public void Skip() => Info.Skip(IsAndroid, "No drag&drop on Android.");

    private static async Task SetupDropzone(IPage page)
    {
        await page.SetContentAsync(@"
    <style>#dropzone { width: 300px; height: 200px; border: 2px dashed #888; }</style>
    <div id=""dropzone""></div>
    <script>
      window.__dropInfo = null;
      const zone = document.getElementById('dropzone');
      zone.addEventListener('dragenter', e => e.preventDefault());
      zone.addEventListener('dragover', e => e.preventDefault());
      zone.addEventListener('drop', async e => {
        e.preventDefault();
        const files = [];
        for (const file of e.dataTransfer.files)
          files.push({ name: file.name, type: file.type, size: file.size, text: await file.text() });
        const data = {};
        for (const t of e.dataTransfer.types) {
          if (t !== 'Files')
            data[t] = e.dataTransfer.getData(t);
        }
        window.__dropInfo = { files, data };
      });
    </script>
  ");
    }

    private static async Task<JsonElement> GetDropInfo(IPage page)
    {
        return await (await page.WaitForFunctionAsync("() => window.__dropInfo")).JsonValueAsync<JsonElement>();
    }

    [UpstreamTest("DD-021 should drop a file payload")]
    public async Task ShouldDropAFilePayload()
    {
        await SetupDropzone(Page);
        await Page.Locator("#dropzone").DropAsync(new()
        {
            Files = new[] { new FilePayload { Name = "note.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") } },
        });
        ExpectToEqual(await GetDropInfo(Page), new
        {
            files = new[] { new { name = "note.txt", type = "text/plain", size = 5, text = "hello" } },
            data = new { },
        });
    }

    [UpstreamTest("DD-022 should drop multiple file payloads")]
    public async Task ShouldDropMultipleFilePayloads()
    {
        await SetupDropzone(Page);
        await Page.Locator("#dropzone").DropAsync(new()
        {
            Files = new[]
            {
                new FilePayload { Name = "a.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("AAA") },
                new FilePayload { Name = "b.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("BB") },
            },
        });
        var info = await GetDropInfo(Page);
        ExpectToEqual(info.GetProperty("files").EnumerateArray().Select(f => new[] { f.GetProperty("name").GetString(), f.GetProperty("text").GetString() }), new[] { new[] { "a.txt", "AAA" }, new[] { "b.txt", "BB" } });
    }

    [UpstreamTest("DD-023 should drop a file by local path")]
    public async Task ShouldDropAFileByLocalPath()
    {
        await SetupDropzone(Page);
        var filePath = Info.OutputPath("hello.txt");
        await File.WriteAllTextAsync(filePath, "path-content");
        await Page.Locator("#dropzone").DropAsync(new() { FilePaths = new[] { filePath } });
        var info = await GetDropInfo(Page);
        Assert.That(info.GetProperty("files").GetArrayLength(), Is.EqualTo(1));
        Assert.That(info.GetProperty("files")[0].GetProperty("name").GetString(), Is.EqualTo("hello.txt"));
        Assert.That(info.GetProperty("files")[0].GetProperty("text").GetString(), Is.EqualTo("path-content"));
    }

    [UpstreamTest("DD-024 should drop clipboard-like data")]
    public async Task ShouldDropClipboardLikeData()
    {
        await SetupDropzone(Page);
        await Page.Locator("#dropzone").DropAsync(new()
        {
            Data = new Dictionary<string, string>
            {
                ["text/plain"] = "hello world",
                ["text/uri-list"] = "https://example.com",
            },
        });
        var info = await GetDropInfo(Page);
        ExpectToEqual(info.GetProperty("files"), Array.Empty<object>());
        Assert.That(info.GetProperty("data").GetProperty("text/plain").GetString(), Is.EqualTo("hello world"));
        Assert.That(info.GetProperty("data").GetProperty("text/uri-list").GetString(), Is.EqualTo("https://example.com"));
    }

    [UpstreamTest("DD-025 should drop files and data together")]
    public async Task ShouldDropFilesAndDataTogether()
    {
        await SetupDropzone(Page);
        await Page.Locator("#dropzone").DropAsync(new()
        {
            Files = new[] { new FilePayload { Name = "mix.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("mix") } },
            Data = new Dictionary<string, string> { ["text/plain"] = "label" },
        });
        var info = await GetDropInfo(Page);
        Assert.That(info.GetProperty("files")[0].GetProperty("text").GetString(), Is.EqualTo("mix"));
        Assert.That(info.GetProperty("data").GetProperty("text/plain").GetString(), Is.EqualTo("label"));
    }

    [UpstreamTest("DD-026 should throw when target does not accept drop")]
    public async Task ShouldThrowWhenTargetDoesNotAcceptDrop()
    {
        // Dropzone without preventDefault on dragover.
        await Page.SetContentAsync(@"
    <div id=""dropzone"" style=""width: 200px; height: 100px;""></div>
  ");
        var error = await ThrowsAsync<PlaywrightException>(() => Page.Locator("#dropzone").DropAsync(new()
        {
            Data = new Dictionary<string, string> { ["text/plain"] = "nope" },
        }));
        Assert.That(error.Message, Does.Match("drop target did not accept the drop").IgnoreCase);
    }

    [UpstreamTest("DD-027 should throw when neither files nor data provided")]
    public async Task ShouldThrowWhenNeitherFilesNorDataProvided()
    {
        await SetupDropzone(Page);
        var error = await ThrowsAsync<PlaywrightException>(() => Page.Locator("#dropzone").DropAsync(new()));
        Assert.That(error.Message, Does.Match("At least one of \"files\" or \"data\""));
    }
}
