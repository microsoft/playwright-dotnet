/**
 * Copyright 2017 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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

/// <summary>Port of tests/page/page-set-input-files.spec.ts.</summary>
public class PageSetInputFilesTests
{
    [UpstreamTest("IF-001 should upload the file")]
    public async Task ShouldUploadTheFile(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), asset("file-to-upload.txt"));
        var input = await page.QuerySelectorAsync("input");
        await input.SetInputFilesAsync(filePath);
        Assert.That(await page.EvaluateAsync<string>("e => e.files[0].name", input), Is.EqualTo("file-to-upload.txt"));
        Assert.That(await page.EvaluateAsync<string>(@"e => {
    const reader = new FileReader();
    const promise = new Promise(fulfill => reader.onload = fulfill);
    reader.readAsText(e.files[0]);
    return promise.then(() => reader.result);
  }", input), Is.EqualTo("contents of the file"));
    }

    [UpstreamTest("IF-002 should upload a folder")]
    public async Task ShouldUploadAFolder(IPage page, Server server, bool isFrozenWebkit, bool isAndroid)
    {
        It.Skip(isAndroid);
        It.Skip(isFrozenWebkit);

        await page.GotoAsync(server.Prefix + "/input/folderupload.html");
        var input = await page.QuerySelectorAsync("input");
        var dir = Path.Combine(It.Info().OutputDir, "file-upload-test");
        {
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, "file1.txt"), "file1 content");
            await File.WriteAllTextAsync(Path.Combine(dir, "file2"), "file2 content");
            Directory.CreateDirectory(Path.Combine(dir, "sub-dir"));
            await File.WriteAllTextAsync(Path.Combine(dir, "sub-dir", "really.txt"), "sub-dir file content");
        }
        await input.SetInputFilesAsync(dir);
        // .NET: toEqual() of two Sets is Is.EquivalentTo.
        Assert.That(new HashSet<string>(await page.EvaluateAsync<string[]>("e => [...e.files].map(f => f.webkitRelativePath)", input)), Is.EquivalentTo(new HashSet<string>
        {
            // Note: this did not work before Chrome 127, see https://issues.chromium.org/issues/345393164.
            "file-upload-test/sub-dir/really.txt",
            "file-upload-test/file1.txt",
            "file-upload-test/file2",
        }));
        var webkitRelativePaths = await page.EvaluateAsync<string[]>("e => [...e.files].map(f => f.webkitRelativePath)", input);
        for (var i = 0; i < webkitRelativePaths.Length; i++)
        {
            var content = await input.EvaluateAsync<string>(@"(e, i) => {
      const reader = new FileReader();
      const promise = new Promise(fulfill => reader.onload = fulfill);
      reader.readAsText(e.files[i]);
      return promise.then(() => reader.result);
    }", i);
            Assert.That(content, Is.EqualTo(File.ReadAllText(Path.Combine(dir, "..", webkitRelativePaths[i]))));
        }
    }

    [UpstreamTest("IF-003 should upload a folder via label")]
    public async Task ShouldUploadAFolderViaLabel(IPage page, bool isFrozenWebkit, bool isAndroid)
    {
        It.Skip(isAndroid);
        It.Skip(isFrozenWebkit);

        await page.SetContentAsync("<label for=\"folder\">Folder</label><input id=\"folder\" type=\"file\" webkitdirectory>");
        var dir = Path.Combine(It.Info().OutputDir, "file-upload-test");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "file1.txt"), "file1 content");
        await page.GetByText("Folder").SetInputFilesAsync(dir);
        Assert.That(await page.Locator("input").EvaluateAsync<string[]>("e => [...e.files].map(f => f.webkitRelativePath)"), Is.EqualTo(new[] { "file-upload-test/file1.txt" }));
    }

    [UpstreamTest("IF-004 should upload a folder and throw for multiple directories")]
    public async Task ShouldUploadAFolderAndThrowForMultipleDirectories(IPage page, Server server, bool isAndroid, bool isFrozenWebkit)
    {
        It.Skip(isAndroid);
        It.Skip(isFrozenWebkit);

        await page.GotoAsync(server.Prefix + "/input/folderupload.html");
        var input = await page.QuerySelectorAsync("input");
        var dir = Path.Combine(It.Info().OutputDir, "file-upload-test");
        {
            Directory.CreateDirectory(Path.Combine(dir, "folder1"));
            await File.WriteAllTextAsync(Path.Combine(dir, "folder1", "file1.txt"), "file1 content");
            Directory.CreateDirectory(Path.Combine(dir, "folder2"));
            await File.WriteAllTextAsync(Path.Combine(dir, "folder2", "file2.txt"), "file2 content");
        }
        var error = await ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(new[]
        {
            Path.Combine(dir, "folder1"),
            Path.Combine(dir, "folder2"),
        }));
        Assert.That(error.Message, Does.Contain("Multiple directories are not supported"));
    }

    [UpstreamTest("IF-005 should throw if a directory and files are passed")]
    public async Task ShouldThrowIfADirectoryAndFilesArePassed(IPage page, Server server, bool isAndroid, bool isFrozenWebkit)
    {
        It.Skip(isAndroid);
        It.Skip(isFrozenWebkit);

        await page.GotoAsync(server.Prefix + "/input/folderupload.html");
        var input = await page.QuerySelectorAsync("input");
        var dir = Path.Combine(It.Info().OutputDir, "file-upload-test");
        {
            Directory.CreateDirectory(Path.Combine(dir, "folder1"));
            await File.WriteAllTextAsync(Path.Combine(dir, "folder1", "file1.txt"), "file1 content");
        }
        var error = await ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(new[]
        {
            Path.Combine(dir, "folder1"),
            Path.Combine(dir, "folder1", "file1.txt"),
        }));
        Assert.That(error.Message, Does.Contain("File paths must be all files or a single directory"));
    }

    [UpstreamTest("IF-006 should throw when uploading a folder in a normal file upload input")]
    public async Task ShouldThrowWhenUploadingAFolderInANormalFileUploadInput(IPage page, Server server, bool isAndroid, bool isFrozenWebkit)
    {
        It.Skip(isAndroid);
        It.Skip(isFrozenWebkit);

        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var input = await page.QuerySelectorAsync("input");
        var dir = Path.Combine(It.Info().OutputDir, "file-upload-test");
        {
            Directory.CreateDirectory(Path.Combine(dir));
            await File.WriteAllTextAsync(Path.Combine(dir, "file1.txt"), "file1 content");
        }
        var error = await ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(dir));
        Assert.That(error.Message, Does.Contain("File input does not support directories, pass individual files instead"));
    }

    [UpstreamTest("IF-007 should throw when uploading a file in a directory upload input")]
    public async Task ShouldThrowWhenUploadingAFileInADirectoryUploadInput(IPage page, Server server, bool isAndroid, Func<string, string> asset, bool isFrozenWebkit)
    {
        It.Skip(isAndroid);
        It.Skip(isFrozenWebkit);

        await page.GotoAsync(server.Prefix + "/input/folderupload.html");
        var input = await page.QuerySelectorAsync("input");
        var error = await ThrowsAsync<PlaywrightException>(() => input.SetInputFilesAsync(asset("file to upload.txt")));
        Assert.That(error.Message, Does.Contain("[webkitdirectory] input requires passing a path to a directory"));
    }

    [UpstreamTest("IF-008 should upload a file after popup")]
    public async Task ShouldUploadAFileAfterPopup(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        {
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("() => window['__popup'] = window.open('about:blank')"));
            await popup.CloseAsync();
        }
        var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), asset("file-to-upload.txt"));
        var input = await page.QuerySelectorAsync("input");
        await input.SetInputFilesAsync(filePath);
        Assert.That(await page.EvaluateAsync<string>("e => e.files[0].name", input), Is.EqualTo("file-to-upload.txt"));
    }

    [UpstreamTest("IF-009 should upload large file")]
    public async Task ShouldUploadLargeFile(IPage page, Server server, bool isAndroid, string mode, TestInfo testInfo)
    {
        It.Skip(isAndroid);
        It.Slow();

        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var uploadFile = testInfo.OutputPath("200MB.zip");
        var str = new string('A', 4 * 1024);
        // .NET: fs.createWriteStream(file) is a StreamWriter, and stream.end() is DisposeAsync().
        var stream = new StreamWriter(uploadFile);
        for (var i = 0; i < 50 * 1024; i++)
        {
            await stream.WriteAsync(str);
        }
        await stream.DisposeAsync();
        var input = page.Locator("input[type=\"file\"]");
        var events = await input.EvaluateHandleAsync(@"e => {
    const events = [];
    e.addEventListener('input', () => events.push('input'));
    e.addEventListener('change', () => events.push('change'));
    return events;
  }");
        await input.SetInputFilesAsync(uploadFile);
        Assert.That(await input.EvaluateAsync<string>("e => e.files[0].name"), Is.EqualTo("200MB.zip"));
        Assert.That(await events.EvaluateAsync<string[]>("e => e"), Is.EqualTo(new[] { "input", "change" }));
        var serverFilePromise = new TaskCompletionSource<FormidableFile>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/upload", async (req, res) =>
        {
            var form = new IncomingForm(new() { UploadDir = testInfo.OutputPath() });
            var (err, fields, f) = await form.Parse(req);
            res.End();
            var files = f;
            serverFilePromise.TrySetResult(files["file1"]);
        });
        var file1 = await PromiseAll(
            serverFilePromise.Task,
            page.ClickAsync("input[type=submit]"));
        Assert.That(file1.OriginalFilename, Is.EqualTo("200MB.zip"));
        Assert.That(file1.Size, Is.EqualTo(200 * 1024 * 1024));
        await PromiseAll(new[] { uploadFile, file1.Filepath }.Select(file => Task.Run(() => File.Delete(file))));
    }

    [UpstreamTest("IF-010 should throw an error if the file does not exist")]

    [Fixme("dotnet: the message is \"Could not find file '<path>/i actually do not exist.txt'.\", the FileNotFoundException of .NET, upstream expects 'ENOENT: no such file or directory'")]
    public async Task ShouldThrowAnErrorIfTheFileDoesNotExist(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var input = await page.QuerySelectorAsync("input");
        var error = await Catch(() => input.SetInputFilesAsync("i actually do not exist.txt"));
        Assert.That(error.Message, Does.Contain("ENOENT: no such file or directory"));
        Assert.That(error.Message, Does.Contain("i actually do not exist.txt"));
    }

    [UpstreamTest("IF-011 should upload large file with relative path")]
    public async Task ShouldUploadLargeFileWithRelativePath(IPage page, Server server, bool isAndroid, string mode, TestInfo testInfo)
    {
        It.Skip(isAndroid);
        It.Slow();

        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var uploadFile = testInfo.OutputPath("200MB.zip");
        var str = new string('A', 4 * 1024);
        // .NET: fs.createWriteStream(file) is a StreamWriter, and stream.end() is DisposeAsync().
        var stream = new StreamWriter(uploadFile);
        for (var i = 0; i < 50 * 1024; i++)
        {
            await stream.WriteAsync(str);
        }
        await stream.DisposeAsync();
        var input = page.Locator("input[type=\"file\"]");
        var events = await input.EvaluateHandleAsync(@"e => {
    const events = [];
    e.addEventListener('input', () => events.push('input'));
    e.addEventListener('change', () => events.push('change'));
    return events;
  }");
        var relativeUploadPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), uploadFile);
        Assert.That(Path.IsPathRooted(relativeUploadPath), Is.False);
        await input.SetInputFilesAsync(relativeUploadPath);
        Assert.That(await input.EvaluateAsync<string>("e => e.files[0].name"), Is.EqualTo("200MB.zip"));
        Assert.That(await events.EvaluateAsync<string[]>("e => e"), Is.EqualTo(new[] { "input", "change" }));
        var serverFilePromise = new TaskCompletionSource<FormidableFile>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/upload", async (req, res) =>
        {
            var form = new IncomingForm(new() { UploadDir = testInfo.OutputPath() });
            var (err, fields, f) = await form.Parse(req);
            res.End();
            var files = f;
            serverFilePromise.TrySetResult(files["file1"]);
        });
        var file1 = await PromiseAll(
            serverFilePromise.Task,
            page.ClickAsync("input[type=submit]"));
        Assert.That(file1.OriginalFilename, Is.EqualTo("200MB.zip"));
        Assert.That(file1.Size, Is.EqualTo(200 * 1024 * 1024));
        await PromiseAll(new[] { uploadFile, file1.Filepath }.Select(file => Task.Run(() => File.Delete(file))));
    }

    [UpstreamTest("IF-012 should upload the file with spaces in name")]
    public async Task ShouldUploadTheFileWithSpacesInName(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), asset("file to upload.txt"));
        var input = await page.QuerySelectorAsync("input");
        await input.SetInputFilesAsync(filePath);
        Assert.That(await page.EvaluateAsync<string>("e => e.files[0].name", input), Is.EqualTo("file to upload.txt"));
        Assert.That(await page.EvaluateAsync<string>(@"e => {
    const reader = new FileReader();
    const promise = new Promise(fulfill => reader.onload = fulfill);
    reader.readAsText(e.files[0]);
    return promise.then(() => reader.result);
  }", input), Is.EqualTo("contents of the file"));
    }

    [UpstreamTest("IF-013 should work @smoke")]
    public async Task ShouldWork(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<input type=file>");
        await page.SetInputFilesAsync("input", asset("file-to-upload.txt"));
        Assert.That(await page.EvalOnSelectorAsync<int>("input", "input => input.files.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"), Is.EqualTo("file-to-upload.txt"));
    }

    [UpstreamTest("IF-014 should set from memory")]
    public async Task ShouldSetFromMemory(IPage page)
    {
        await page.SetContentAsync("<input type=file>");
        await page.SetInputFilesAsync("input", new FilePayload
        {
            Name = "test.txt",
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes("this is a test"),
        });
        Assert.That(await page.EvalOnSelectorAsync<int>("input", "input => input.files.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"), Is.EqualTo("test.txt"));
    }

    [UpstreamTest("IF-015 should work with CSP")]
    public async Task ShouldWorkWithCSP(IPage page, Server server, Func<string, string> asset)
    {
        server.SetCSP("/empty.html", "default-src \"none\"");
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<input type=file>");
        await page.SetInputFilesAsync("input", asset("file-to-upload.txt"));
        Assert.That(await page.EvalOnSelectorAsync<int>("input", "input => input.files.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"), Is.EqualTo("file-to-upload.txt"));
    }

    [UpstreamTest("IF-016 should detect mime type")]
    public async Task ShouldDetectMimeType(IPage page, Server server, Func<string, string> asset)
    {

        FormidableMap<FormidableFile> files = null;
        server.SetRoute("/upload", async (req, res) =>
        {
            var form = new IncomingForm();
            var (err, fields, f) = await form.Parse(req);
            files = f;
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <form action=""/upload"" method=""post"" enctype=""multipart/form-data"" >
      <input type=""file"" name=""file1"">
      <input type=""file"" name=""file2"">
      <input type=""submit"" value=""Submit"">
    </form>");
        await (await page.QuerySelectorAsync("input[name=file1]")).SetInputFilesAsync(asset("file-to-upload.txt"));
        await (await page.QuerySelectorAsync("input[name=file2]")).SetInputFilesAsync(asset("pptr.png"));
        await PromiseAll(
            page.ClickAsync("input[type=submit]"),
            server.WaitForRequest("/upload"));
        var (file1, file2) = (files["file1"], files["file2"]);
        Assert.That(file1.OriginalFilename, Is.EqualTo("file-to-upload.txt"));
        Assert.That(file1.Mimetype, Is.EqualTo("text/plain"));
        Assert.That(Encoding.UTF8.GetString(File.ReadAllBytes(file1.Filepath)), Is.EqualTo(
            Encoding.UTF8.GetString(File.ReadAllBytes(asset("file-to-upload.txt")))));
        Assert.That(file2.OriginalFilename, Is.EqualTo("pptr.png"));
        Assert.That(file2.Mimetype, Is.EqualTo("image/png"));
        Assert.That(Encoding.UTF8.GetString(File.ReadAllBytes(file2.Filepath)), Is.EqualTo(
            Encoding.UTF8.GetString(File.ReadAllBytes(asset("pptr.png")))));
    }

    // @see https://github.com/microsoft/playwright/issues/4704
    [UpstreamTest("IF-017 should not trim big uploaded files")]
    public async Task ShouldNotTrimBigUploadedFiles(IPage page, Server server)
    {

        FormidableMap<FormidableFile> files = null;
        server.SetRoute("/upload", async (req, res) =>
        {
            var form = new IncomingForm();
            var (err, fields, f) = await form.Parse(req);
            files = f;
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);

        var DATA_SIZE = Math.Pow(2, 20);
        await PromiseAll(
            page.EvaluateAsync(@"async size => {
      const body = new FormData();
      body.set('file', new Blob([new Uint8Array(size)]));
      await fetch('/upload', { method: 'POST', body });
    }", DATA_SIZE),
            server.WaitForRequest("/upload"));
        Assert.That(files["file"].Size, Is.EqualTo(DATA_SIZE));
    }

    [UpstreamTest("IF-018 should emit input and change events")]
    public async Task ShouldEmitInputAndChangeEvents(IPage page, Func<string, string> asset)
    {
        var events = new List<JsonElement>();
        await page.ExposeFunctionAsync("eventHandled", (JsonElement e) => events.Add(e));
        await page.SetContentAsync(@"
  <input id=input type=file></input>
  <script>
    input.addEventListener('input', e => eventHandled({ type: e.type }));
    input.addEventListener('change', e => eventHandled({ type: e.type }));
  </script>");
        await (await page.QuerySelectorAsync("input")).SetInputFilesAsync(asset("file-to-upload.txt"));
        Assert.That(events.Count, Is.EqualTo(2));
        Assert.That(events[0].GetProperty("type").GetString(), Is.EqualTo("input"));
        Assert.That(events[1].GetProperty("type").GetString(), Is.EqualTo("change"));
    }

    [UpstreamTest("IF-019 input event.composed should be true and cross shadow dom boundary")]
    public async Task InputEventComposedShouldBeTrueAndCrossShadowDomBoundary(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"<body><script>
  const div = document.createElement('div');
  const shadowRoot = div.attachShadow({mode: 'open'});
  shadowRoot.innerHTML = '<input type=file></input>';
  document.body.appendChild(div);
</script></body>");
        await page.Locator("body").EvaluateAsync(@"select => {
    window.firedBodyEvents = [];
    for (const event of ['input', 'change']) {
      select.addEventListener(event, e => {
        window.firedBodyEvents.push(e.type + ':' + e.composed);
      }, false);
    }
  }");

        await page.Locator("input").EvaluateAsync(@"select => {
    window.firedEvents = [];
    for (const event of ['input', 'change']) {
      select.addEventListener(event, e => {
        window.firedEvents.push(e.type + ':' + e.composed);
      }, false);
    }
  }");
        await page.Locator("input").SetInputFilesAsync(new FilePayload
        {
            Name = "test.txt",
            MimeType = "text/plain",
            Buffer = Encoding.UTF8.GetBytes("this is a test"),
        });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['firedEvents']"), Is.EqualTo(new[] { "input:true", "change:false" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['firedBodyEvents']"), Is.EqualTo(new[] { "input:true" }));
    }

    [UpstreamTest("IF-020 input should trigger events when files changed second time")]
    public async Task InputShouldTriggerEventsWhenFilesChangedSecondTime(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<input type=file multiple=true/>");

        var input = page.Locator("input");
        var events = await input.EvaluateHandleAsync(@"e => {
    const events = [];
    e.addEventListener('input', () => events.push('input'));
    e.addEventListener('change', () => events.push('change'));
    return events;
  }");

        await input.SetInputFilesAsync(asset("file-to-upload.txt"));
        Assert.That(await input.EvaluateAsync<string>("e => e.files[0].name"), Is.EqualTo("file-to-upload.txt"));
        Assert.That(await events.EvaluateAsync<string[]>("e => e"), Is.EqualTo(new[] { "input", "change" }));

        await events.EvaluateAsync("e => e.length = 0");

        await input.SetInputFilesAsync(asset("pptr.png"));
        Assert.That(await input.EvaluateAsync<string>("e => e.files[0].name"), Is.EqualTo("pptr.png"));
        Assert.That(await events.EvaluateAsync<string[]>("e => e"), Is.EqualTo(new[] { "input", "change" }));
    }

    [UpstreamTest("IF-021 should preserve lastModified timestamp")]
    public async Task ShouldPreserveLastModifiedTimestamp(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<input type=file multiple=true/>");
        var input = page.Locator("input");
        var files = new[] { "file-to-upload.txt", "file-to-upload-2.txt" };
        await input.SetInputFilesAsync(files.Select(f => asset(f)));
        Assert.That(await input.EvaluateAsync<string[]>("e => [...e.files].map(f => f.name)"), Is.EqualTo(files));
        var timestamps = await input.EvaluateAsync<double[]>("e => [...e.files].map(f => f.lastModified)");
        // .NET: fs.statSync(file).mtimeMs is the last write time in milliseconds since the Unix epoch.
        var expectedTimestamps = files.Select(file => Math.Round((File.GetLastWriteTimeUtc(asset(file)) - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero)).ToArray();
        // On Linux browser sometimes reduces the timestamp by 1ms: 1696272058110.0715  -> 1696272058109 or even
        // rounds it to seconds in WebKit: 1696272058110 -> 1696272058000.
        for (var i = 0; i < timestamps.Length; i++)
        {
            Assert.That(Math.Abs(timestamps[i] - expectedTimestamps[i]), Is.LessThanOrEqualTo(1000), $"expected: {string.Join(",", expectedTimestamps)}; actual: {string.Join(",", timestamps)}");
        }
    }

    [UpstreamTest("IF-022 should store an uploaded file in IndexedDB")]
    public async Task ShouldStoreAnUploadedFileInIndexedDB(IPage page, Server server, Func<string, string> asset, string browserName)
    {
        It.Fixme(browserName == "webkit", "WebKit does not support storing files in IndexedDB in private browsing.");

        await page.GotoAsync(server.Prefix + "/input/fileupload.html");
        var input = page.Locator("input[type=file]");
        await input.SetInputFilesAsync(asset("file-to-upload.txt"));
        var storedFile = await input.EvaluateAsync<object>(@"async input => {
    const db = await new Promise((resolve, reject) => {
      const request = indexedDB.open('file-upload');
      request.onupgradeneeded = () => request.result.createObjectStore('files');
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    const file = await new Promise((resolve, reject) => {
      const transaction = db.transaction('files', 'readwrite');
      const store = transaction.objectStore('files');
      store.put(input.files[0], 'file');
      const request = store.get('file');
      transaction.oncomplete = () => resolve(request.result);
      transaction.onabort = () => reject(transaction.error);
    });
    db.close();
    return { name: file.name, text: await file.text() };
  }");
        ExpectToEqual(storedFile, new { name = "file-to-upload.txt", text = "contents of the file" });
    }
}
