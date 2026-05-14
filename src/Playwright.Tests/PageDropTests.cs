/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text;

namespace Microsoft.Playwright.Tests;

public class PageDropTests : PageTestEx
{
    private const string DropzoneSetup = @"
        <style>#dropzone { width: 300px; height: 200px; border: 2px dashed #888; }</style>
        <div id='dropzone'></div>
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
        </script>";

    [PlaywrightTest("page-drop.spec.ts", "should drop a file payload")]
    public async Task ShouldDropFilePayload()
    {
        await Page.SetContentAsync(DropzoneSetup);
        await Page.Locator("#dropzone").DropAsync(new()
        {
            Files = new[]
            {
                new FilePayload { Name = "note.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") },
            },
        });
        var info = await Page.EvaluateAsync<DropInfo>("() => window.__dropInfo");
        Assert.AreEqual(1, info.Files.Length);
        Assert.AreEqual("note.txt", info.Files[0].Name);
        Assert.AreEqual("text/plain", info.Files[0].Type);
        Assert.AreEqual("hello", info.Files[0].Text);
    }

    [PlaywrightTest("page-drop.spec.ts", "should drop clipboard-like data")]
    public async Task ShouldDropClipboardLikeData()
    {
        await Page.SetContentAsync(DropzoneSetup);
        await Page.Locator("#dropzone").DropAsync(new()
        {
            Data = new Dictionary<string, string>
            {
                ["text/plain"] = "hello world",
                ["text/uri-list"] = "https://example.com",
            },
        });
        var hasData = await Page.EvaluateAsync<bool>("() => !!window.__dropInfo");
        Assert.IsTrue(hasData);
    }

    [PlaywrightTest("page-drop.spec.ts", "should throw when target does not accept drop")]
    public async Task ShouldThrowWhenTargetRejects()
    {
        await Page.SetContentAsync("<div id='dropzone' style='width: 200px; height: 100px;'></div>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("#dropzone").DropAsync(new()
        {
            Data = new Dictionary<string, string> { ["text/plain"] = "nope" },
        }));
        StringAssert.Contains("drop target did not accept the drop", exception.Message.ToLowerInvariant());
    }

    public class DropInfo
    {
        public DropFile[] Files { get; set; } = System.Array.Empty<DropFile>();
        public Dictionary<string, string> Data { get; set; } = new();
    }

    public class DropFile
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Size { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
