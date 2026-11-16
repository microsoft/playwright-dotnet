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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-filechooser.spec.ts.</summary>
public class PageFilechooserTests
{
    [UpstreamTest("FC-001 should upload multiple large files")]
    public async Task ShouldUploadMultipleLargeFiles(IPage page, Server server, bool isAndroid, string mode, TestInfo testInfo)
    {
        It.Skip(isAndroid);
        It.Slow();

        var filesCount = 10;
        await page.GotoAsync(server.Prefix + "/input/fileupload-multi.html");
        var uploadFile = testInfo.OutputPath("50MB_1.zip");
        var str = new string('A', 1024);
        var stream = new StreamWriter(uploadFile);
        // 49 is close to the actual limit
        for (var i = 0; i < 49 * 1024; i++)
        {
            await stream.WriteAsync(str);
        }
        await stream.DisposeAsync();
        var input = page.Locator("input[type=\"file\"]");
        var uploadFiles = new List<string> { uploadFile };
        for (var i = 2; i <= filesCount; i++)
        {
            var dstFile = testInfo.OutputPath($"50MB_{i}.zip");
            File.Copy(uploadFile, dstFile, true);
            uploadFiles.Add(dstFile);
        }
        var fileChooserPromise = page.WaitForFileChooserAsync();
        await input.ClickAsync();
        var fileChooser = await fileChooserPromise;
        await fileChooser.SetFilesAsync(uploadFiles);
        var filesLen = await page.EvaluateAsync<int>("document.getElementsByTagName(\"input\")[0].files.length");
        Assert.That(fileChooser.IsMultiple, Is.EqualTo(true));
        Assert.That(filesLen, Is.EqualTo(filesCount));
        await PromiseAll(uploadFiles.Select(path => Task.Run(() => File.Delete(path))));
    }

    [UpstreamTest("FC-002 should emit event once")]
    public async Task ShouldEmitEventOnce(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=file>");
        var chooser = await PromiseAll(
            Once<IFileChooser>(h => page.FileChooser += h, h => page.FileChooser -= h),
            page.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("FC-004 should emit event for iframe")]
    public async Task ShouldEmitEventForIframe(IPage page, Server server)
    {
        var frame = await AttachFrame(page, "frame1", server.EmptyPage);
        await frame.SetContentAsync("<input type=file>");
        var chooser = await PromiseAll(
            Once<IFileChooser>(h => page.FileChooser += h, h => page.FileChooser -= h),
            frame.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("FC-005 should emit event on/off")]
    public async Task ShouldEmitEventOnOff(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=file>");
        // .NET: new Promise(f => { ... }) is a TaskCompletionSource that the listener settles.
        var f = new TaskCompletionSource<IFileChooser>(TaskCreationOptions.RunContinuationsAsynchronously);
        {
            EventHandler<IFileChooser> listener = null;
            listener = (_, chooser) =>
            {
                page.FileChooser -= listener;
                f.SetResult(chooser);
            };
            page.FileChooser += listener;
        }
        var chooser = await PromiseAll(
            f.Task,
            page.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("FC-006 should emit event addListener/removeListener")]
    public async Task ShouldEmitEventAddListenerRemoveListener(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=file>");
        // .NET: new Promise(f => { ... }) is a TaskCompletionSource that the listener settles.
        var f = new TaskCompletionSource<IFileChooser>(TaskCreationOptions.RunContinuationsAsynchronously);
        {
            EventHandler<IFileChooser> listener = null;
            listener = (_, chooser) =>
            {
                page.FileChooser -= listener;
                f.SetResult(chooser);
            };
            page.FileChooser += listener;
        }
        var chooser = await PromiseAll(
            f.Task,
            page.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("FC-007 should work when file input is attached to DOM")]
    public async Task ShouldWorkWhenFileInputIsAttachedToDOM(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=file>");
        var chooser = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("FC-008 should work when file input is not attached to DOM")]
    public async Task ShouldWorkWhenFileInputIsNotAttachedToDOM(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<button>Open</button>");
        await page.EvaluateAsync(@"() => {
    document.querySelector('button').onclick = () => {
      const input = document.createElement('input');
      input.type = 'file';
      window['events'] = [];
      for (const type of ['cancel', 'input', 'change'])
        input.addEventListener(type, () => window['events'].push(`${type} ${input.files.length}`));
      input.click();
    };
  }");
        var chooser = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("button"));
        await chooser.SetFilesAsync(asset("file-to-upload.txt"));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(new[] { "input 1", "change 1" }));
        var content = await chooser.Element.EvaluateAsync<string>(@"input => {
    const reader = new FileReader();
    const promise = new Promise(fulfill => reader.onload = fulfill);
    reader.readAsText(input.files[0]);
    return promise.then(() => reader.result);
  }");
        Assert.That(content, Is.EqualTo("contents of the file"));
    }

    [UpstreamTest("FC-009 should not throw when filechooser belongs to iframe")]
    public async Task ShouldNotThrowWhenFilechooserBelongsToIframe(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        var frame = page.MainFrame.ChildFrames[0];
        await frame.SetContentAsync(@"
    <div>Click me</div>
    <script>
      document.querySelector('div').addEventListener('click', () => {
        const input = document.createElement('input');
        input.type = 'file';
        input.click();
        window.parent.__done = true;
      });
    </script>
  ");
        await PromiseAll(
            page.WaitForFileChooserAsync(),
            frame.ClickAsync("div"));
        await page.WaitForFunctionAsync("() => window.__done");
    }

    [UpstreamTest("FC-010 should not throw when frame is detached immediately")]
    public async Task ShouldNotThrowWhenFrameIsDetachedImmediately(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        var frame = page.MainFrame.ChildFrames[0];
        await frame.SetContentAsync(@"
    <div>Click me</div>
    <script>
      document.querySelector('div').addEventListener('click', () => {
        const input = document.createElement('input');
        input.type = 'file';
        input.click();
        window.parent.__done = true;
        const iframe = window.parent.document.querySelector('iframe');
        iframe.remove();
      });
    </script>
  ");
        page.FileChooser += (_, _) => { };  // To ensure we handle file choosers.
        await frame.ClickAsync("div");
        await page.WaitForFunctionAsync("() => window.__done");
    }

    [UpstreamTest("FC-011 should respect timeout")]
    public async Task ShouldRespectTimeout(IPage page, IPlaywright playwright)
    {
        Exception error = null;
        error = await Catch(() => page.WaitForFileChooserAsync(new() { Timeout = 1 }));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("FC-012 should respect default timeout when there is no custom timeout")]
    public async Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout(IPage page, IPlaywright playwright)
    {
        page.SetDefaultTimeout(1);
        Exception error = null;
        error = await Catch(() => page.WaitForFileChooserAsync());
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("FC-013 should prioritize exact timeout over default timeout")]
    public async Task ShouldPrioritizeExactTimeoutOverDefaultTimeout(IPage page, IPlaywright playwright)
    {
        page.SetDefaultTimeout(0);
        Exception error = null;
        error = await Catch(() => page.WaitForFileChooserAsync(new() { Timeout = 1 }));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("FC-014 should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout(IPage page, Server server)
    {
        var (chooser, _) = await PromiseAll(
            page.WaitForFileChooserAsync(new() { Timeout = 0 }),
            page.EvaluateAsync(@"() => window.builtins.setTimeout(() => {
      const el = document.createElement('input');
      el.type = 'file';
      el.click();
    }, 50)"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("FC-016 should return the same file chooser when there are many watchdogs simultaneously")]
    public async Task ShouldReturnTheSameFileChooserWhenThereAreManyWatchdogsSimultaneously(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=file>");
        var (fileChooser1, fileChooser2, _) = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.WaitForFileChooserAsync(),
            page.EvalOnSelectorAsync("input", "input => input.click()"));
        Assert.That(ReferenceEquals(fileChooser1, fileChooser2), Is.EqualTo(true));
    }

    [UpstreamTest("FC-017 should accept single file")]
    public async Task ShouldAcceptSingleFile(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<input type=file oninput='javascript:console.timeStamp()'>");
        var fileChooser = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        Assert.That(fileChooser.Page, Is.SameAs(page));
        Assert.That(fileChooser.Element, Is.Not.Null);
        await fileChooser.SetFilesAsync(asset("file-to-upload.txt"));
        Assert.That(await page.EvalOnSelectorAsync<int>("input", "input => input.files.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"), Is.EqualTo("file-to-upload.txt"));
    }

    [UpstreamTest("FC-018 should not trim big uploaded files")]
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

    [UpstreamTest("FC-019 should be able to read selected file")]
    public async Task ShouldBeAbleToReadSelectedFile(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<input type=file>");
        // .NET: page.waitForEvent('filechooser').then(fileChooser => fileChooser.setFiles(...)) is a local function that is called in its place.
        async Task Then()
        {
            var fileChooser = await page.WaitForFileChooserAsync();
            await fileChooser.SetFilesAsync(asset("file-to-upload.txt"));
        }
        var content = await PromiseAll(
            Then(),
            page.EvalOnSelectorAsync<string>("input", @"async picker => {
      picker.click();
      await new Promise(x => picker.oninput = x);
      const reader = new FileReader();
      const promise = new Promise(fulfill => reader.onload = fulfill);
      reader.readAsText(picker.files[0]);
      return promise.then(() => reader.result);
    }"));
        Assert.That(content, Is.EqualTo("contents of the file"));
    }

    [UpstreamTest("FC-020 should be able to reset selected files with empty file list")]
    public async Task ShouldBeAbleToResetSelectedFilesWithEmptyFileList(IPage page, Func<string, string> asset)
    {
        await page.SetContentAsync("<input type=file>");
        // .NET: page.waitForEvent('filechooser').then(fileChooser => fileChooser.setFiles(...)) is a local function that is called in its place.
        async Task Then1()
        {
            var fileChooser = await page.WaitForFileChooserAsync();
            await fileChooser.SetFilesAsync(asset("file-to-upload.txt"));
        }
        var fileLength1 = await PromiseAll(
            Then1(),
            page.EvalOnSelectorAsync<int>("input", @"async picker => {
      picker.click();
      await new Promise(x => picker.oninput = x);
      return picker.files.length;
    }"));
        Assert.That(fileLength1, Is.EqualTo(1));
        // .NET: page.waitForEvent('filechooser').then(fileChooser => fileChooser.setFiles([])) is a local function that is called in its place.
        async Task Then2()
        {
            var fileChooser = await page.WaitForFileChooserAsync();
            await fileChooser.SetFilesAsync(Array.Empty<string>());
        }
        var fileLength2 = await PromiseAll(
            Then2(),
            page.EvalOnSelectorAsync<int>("input", @"async picker => {
      picker.click();
      await new Promise(x => picker.oninput = x);
      return picker.files.length;
    }"));
        Assert.That(fileLength2, Is.EqualTo(0));
    }

    [UpstreamTest("FC-021 should work for single file pick")]
    public async Task ShouldWorkForSingleFilePick(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=file>");
        var fileChooser = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        Assert.That(fileChooser.IsMultiple, Is.EqualTo(false));
    }

    [UpstreamTest("FC-022 should work for \"multiple\"")]
    public async Task ShouldWorkForMultiple(IPage page, Server server)
    {
        await page.SetContentAsync("<input multiple type=file>");
        var fileChooser = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        Assert.That(fileChooser.IsMultiple, Is.EqualTo(true));
    }

    [UpstreamTest("FC-023 should work for \"webkitdirectory\"")]
    public async Task ShouldWorkForWebkitdirectory(IPage page, Server server)
    {
        await page.SetContentAsync("<input multiple webkitdirectory type=file>");
        var fileChooser = await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        Assert.That(fileChooser.IsMultiple, Is.EqualTo(true));
    }

    [UpstreamTest("FC-024 should emit event after navigation")]
    public async Task ShouldEmitEventAfterNavigation(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "chromium" && browserMajorVersion < 99);

        var logs = new List<string>();
        page.FileChooser += (_, _) => logs.Add("filechooser");
        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.SetContentAsync("<input type=file>");
        await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        await page.SetContentAsync("<input type=file>");
        await PromiseAll(
            page.WaitForFileChooserAsync(),
            page.ClickAsync("input"));
        Assert.That(logs, Is.EqualTo(new[] { "filechooser", "filechooser" }));
    }

    [UpstreamTest("FC-025 should trigger listener added before navigation")]
    public async Task ShouldTriggerListenerAddedBeforeNavigation(IPage page, Server server, int browserMajorVersion, bool isElectron)
    {
        It.Skip(isElectron && browserMajorVersion <= 98);
        // Add listener before cross process navigation.
        var chooserPromise = Once<IFileChooser>(h => page.FileChooser += h, h => page.FileChooser -= h);
        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        await page.SetContentAsync("<input type=file>");
        var chooser = await PromiseAll(
            chooserPromise,
            page.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    public class UserActivation
    {
        // Each test navigates to a fresh document, because transient activation
        // (including the one granted by page.evaluate in all browsers) sticks to
        // the document for several seconds.
        private const string html = @"
    <button id=""once"">once</button>
    <button id=""twice"">twice</button>
    <input type=file>
    <script>
      const input = document.querySelector('input');
      document.querySelector('#once').addEventListener('click', () => input.click());
      document.querySelector('#twice').addEventListener('click', () => { input.click(); input.click(); });
      window.showPicker = () => {
        try {
          input.showPicker();
          return 'ok';
        } catch (e) {
          return e.name;
        }
      };
      if (location.search === '?click')
        setTimeout(() => input.click(), 0);
      if (location.search === '?showPicker')
        setTimeout(() => console.log('showPicker:' + window.showPicker()), 0);
    </script>
  ";

        [BeforeEach]
        public async Task BeforeEach(IPage page, Server server)
        {
            await page.RouteAsync(server.Prefix + "/activation.html*", route => route.FulfillAsync(new() { ContentType = "text/html", Body = html }));
        }

        [UpstreamTest("FC-026 should not open file chooser without user activation")]
        public async Task ShouldNotOpenFileChooserWithoutUserActivation(IPage page, Server server, string browserName)
        {
            It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
            It.Fixme(browserName == "webkit", "WebKit opens the file chooser from input.click() without user activation");
            var choosers = 0;
            page.FileChooser += (_, _) => choosers++;

            await page.GotoAsync(server.Prefix + "/activation.html?click");
            await page.WaitForTimeoutAsync(500);
            Assert.That(choosers, Is.EqualTo(0));

            // Do not poll with waitForFunction: each poll evaluates in the page with a user gesture,
            // which grants transient user activation and would let showPicker() succeed.
            var result = page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text.StartsWith("showPicker:", StringComparison.Ordinal) });
            await page.GotoAsync(server.Prefix + "/activation.html?showPicker");
            Assert.That((await result).Text, Is.EqualTo("showPicker:NotAllowedError"));
            Assert.That(choosers, Is.EqualTo(0));
        }

        [UpstreamTest("FC-027 should open file chooser with user activation")]
        public async Task ShouldOpenFileChooserWithUserActivation(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/activation.html");
            var chooser = await PromiseAll(
                page.WaitForFileChooserAsync(),
                page.ClickAsync("#once"));
            Assert.That(chooser, Is.Not.Null);

            // page.evaluate grants transient user activation in all browsers.
            await page.GotoAsync(server.Prefix + "/activation.html");
            var (chooser2, _) = await PromiseAll(
                page.WaitForFileChooserAsync(),
                page.EvaluateAsync("() => document.querySelector('input').click()"));
            Assert.That(chooser2, Is.Not.Null);
            await page.GotoAsync(server.Prefix + "/activation.html");
            Assert.That(await page.EvaluateAsync<string>("() => window.showPicker()"), Is.EqualTo("ok"));
        }

        [UpstreamTest("FC-028 should consume user activation when opening file chooser")]
        public async Task ShouldConsumeUserActivationWhenOpeningFileChooser(IPage page, Server server, string browserName, bool isElectron, bool isAndroid)
        {
            It.Fixme(browserName == "webkit", "WebKit does not consume user activation when opening the file chooser");
            It.Fixme(isElectron || isAndroid, "Chromium 148 in Electron 42 and Chrome on the Android emulator do not consume user activation");
            var choosers = 0;
            page.FileChooser += (_, _) => choosers++;
            await page.GotoAsync(server.Prefix + "/activation.html");
            await page.ClickAsync("#twice");
            await page.WaitForTimeoutAsync(500);
            Assert.That(choosers, Is.EqualTo(1));
        }
    }
}
