using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>page-set-input-files.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class PageSetInputFilesTests : PageTestEx
    {
        [PlaywrightTest("page-set-input-files.spec.ts", "should upload the file")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUploadTheFile()
        {
            await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload);
            var input = await Page.QuerySelectorAsync("input");
            await input.SetInputFilesAsync(filePath);
            Assert.AreEqual("file-to-upload.txt", await Page.EvaluateAsync<string>("e => e.files[0].name", input));
            Assert.AreEqual("contents of the file", await Page.EvaluateAsync<string>(@"e => {
                var reader = new FileReader();
                var promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(e.files[0]);
                return promise.then(() => reader.result);
            }", input));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<input type=file>");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload);
            await Page.SetInputFilesAsync("input", filePath);

            Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
            Assert.AreEqual("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should set from memory")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetFromMemory()
        {
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", new FilePayload
            {
                Name = "test.txt",
                MimeType = "text/plain",
                Buffer = Encoding.UTF8.GetBytes("this is a test"),
            });

            Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
            Assert.AreEqual("test.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit event once")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitEventOnce()
        {
            await Page.SetContentAsync("<input type=file>");
            var chooserTsc = new TaskCompletionSource<IElementHandle>();
            void EventHandler(object sender, IFileChooser e)
            {
                chooserTsc.SetResult(e.Element);
                Page.FileChooser -= EventHandler;
            }
            Page.FileChooser += EventHandler;
            var chooser = await TaskUtils.WhenAll(
                chooserTsc.Task,
                Page.ClickAsync("input")
            );
            Assert.NotNull(chooser);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit event on/off")]
        [Test, Ignore("We dont'need to test this")]
        public void ShouldEmitEventOnOff()
        {
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit addListener/removeListener")]
        [Test, Ignore("We dont'need to test this")]
        public void ShouldEmitEventAddListenerRemoveListener()
        {
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work when file input is attached to DOM")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenFileInputIsAttachedToDOM()
        {
            await Page.SetContentAsync("<input type=file>");
            var chooser = await TaskUtils.WhenAll(
                Page.WaitForFileChooserAsync(),
                Page.ClickAsync("input")
            );
            Assert.NotNull(chooser?.Element);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work when file input is not attached to DOM")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenFileInputIsNotAttachedToDOM()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForFileChooserAsync(),
                Page.EvaluateAsync(@"() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }")
            );
            Assert.NotNull(chooser?.Element);
        }


        [PlaywrightTest("page-set-input-files.spec.ts", "should work with CSP")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "default-src \"none\"");
            await Page.GotoAsync(Server.EmptyPage);
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload));
            Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
            Assert.AreEqual("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public Task ShouldRespectTimeout()
        {
            return PlaywrightAssert.ThrowsAsync<TimeoutException>(()
             => Page.WaitForFileChooserAsync(new() { Timeout = 1 }));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should respect default timeout when there is no custom timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout()
        {
            Page.SetDefaultTimeout(1);
            return PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForFileChooserAsync());
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should prioritize exact timeout over default timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public Task ShouldPrioritizeExactTimeoutOverDefaultTimeout()
        {
            Page.SetDefaultTimeout(0);
            return PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForFileChooserAsync(new() { Timeout = 1 }));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work with no timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForFileChooserAsync(new() { Timeout = 0 }),
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }, 50)")
            );
            Assert.NotNull(chooser?.Element);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should return the same file chooser when there are many watchdogs simultaneously")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheSameFileChooserWhenThereAreManyWatchdogsSimultaneously()
        {
            await Page.SetContentAsync("<input type=file>");
            var (fileChooser1, fileChooser2, _) = await TaskUtils.WhenAll(
                Page.WaitForFileChooserAsync(),
                Page.WaitForFileChooserAsync(),
                Page.EvalOnSelectorAsync("input", "input => input.click()")
            );
            Assert.AreEqual(fileChooser1, fileChooser2);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should accept single file")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptSingleFile()
        {
            await Page.SetContentAsync("<input type=file oninput='javascript:console.timeStamp()'>");
            var fileChooser = await Page.RunAndWaitForFileChooserAsync(async () =>
            {
                await Page.ClickAsync("input");
            });

            Assert.AreEqual(Page, fileChooser.Page);
            Assert.NotNull(fileChooser.Element);
            await fileChooser.SetFilesAsync(TestConstants.FileToUpload);
            Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
            Assert.AreEqual("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should detect mime type")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDetectMimeType()
        {
            var files = new List<(string name, string mime, byte[] content)>();

            Server.SetRoute("/upload", context =>
            {
                files.AddRange(context.Request.Form.Files.Select(f =>
                {
                    using var ms = new MemoryStream();
                    f.CopyTo(ms);
                    return (f.FileName, f.ContentType, ms.ToArray());
                }));
                return Task.CompletedTask;
            });

            await Page.GotoAsync(Server.EmptyPage);
            await Page.SetContentAsync(@"
                <form action=""/upload"" method=""post"" enctype=""multipart/form-data"">
                    <input type=""file"" name=""file1"">
                    <input type=""file"" name=""file2"">
                    <input type=""submit"" value=""Submit"">
                </form>
            ");

            await (await Page.QuerySelectorAsync("input[name=file1]")).SetInputFilesAsync(TestUtils.GetWebServerFile("file-to-upload.txt"));
            await (await Page.QuerySelectorAsync("input[name=file2]")).SetInputFilesAsync(TestUtils.GetWebServerFile("pptr.png"));

            await TaskUtils.WhenAll(
               Page.ClickAsync("input[type=submit]"),
               Server.WaitForRequest("/upload")
            );

            Assert.AreEqual("file-to-upload.txt", files[0].name);
            Assert.AreEqual("text/plain", files[0].mime);
            Assert.AreEqual(File.ReadAllBytes(TestUtils.GetWebServerFile("file-to-upload.txt")), files[0].content);

            Assert.AreEqual("pptr.png", files[1].name);
            Assert.AreEqual("image/png", files[1].mime);
            Assert.AreEqual(File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png")), files[1].content);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should be able to read selected file")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToReadSelectedFile()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForFileChooserAsync()
                .ContinueWith(task => task.Result.SetFilesAsync(TestConstants.FileToUpload));
            Assert.AreEqual("contents of the file", await Page.EvalOnSelectorAsync<string>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                const reader = new FileReader();
                const promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(picker.files[0]);
                return promise.then(() => reader.result);
            }"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should be able to reset selected files with empty file list")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToResetSelectedFilesWithEmptyFileList()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForFileChooserAsync()
                .ContinueWith(task => task.Result.SetFilesAsync(TestConstants.FileToUpload));
            Assert.AreEqual(1, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
            _ = Page.WaitForFileChooserAsync()
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync(new string[] { }));
            Assert.AreEqual(0, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should not accept multiple files for single-file input")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAcceptMultipleFilesForSingleFileInput()
        {
            await Page.SetContentAsync("<input type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForFileChooserAsync(),
               Page.ClickAsync("input")
            );
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
                fileChooser.SetFilesAsync(new string[]
                {
                    TestUtils.GetWebServerFile(TestConstants.FileToUpload),
                    TestUtils.GetWebServerFile("pptr.png"),
                }));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit input and change events")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitInputAndChangeEvents()
        {
            var events = new List<string>();
            await Page.ExposeFunctionAsync("eventHandled", (string e) => events.Add(e));

            await Page.SetContentAsync(@"
                 <input id=input type=file></input>
                <script>
                  input.addEventListener('input', e => eventHandled(e.type));
                  input.addEventListener('change', e => eventHandled(e.type));
                </script>
            ");

            await (await Page.QuerySelectorAsync("input")).SetInputFilesAsync(TestUtils.GetWebServerFile("file-to-upload.txt"));
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("input", events[0]);
            Assert.AreEqual("change", events[1]);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work for single file pick")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForSingleFilePick()
        {
            await Page.SetContentAsync("<input type=file>");
            var waitTask = Page.WaitForFileChooserAsync();

            var fileChooser = await TaskUtils.WhenAll(
               waitTask,
               Page.ClickAsync("input")
            );
            Assert.False(fileChooser.IsMultiple);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", @"should work for ""multiple""")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMultiple()
        {
            await Page.SetContentAsync("<input multiple type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForFileChooserAsync(),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.IsMultiple);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", @"should work for ""webkitdirectory""")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForWebkitdirectory()
        {
            await Page.SetContentAsync("<input multiple webkitdirectory type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForFileChooserAsync(),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.IsMultiple);
        }
    }
}
