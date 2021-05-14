using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>page-set-input-files.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetInputFilesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetInputFilesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should upload the file")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUploadTheFile()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/fileupload.html");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload);
            var input = await Page.QuerySelectorAsync("input");
            await input.SetInputFilesAsync(filePath);
            Assert.Equal("file-to-upload.txt", await Page.EvaluateAsync<string>("e => e.files[0].name", input));
            Assert.Equal("contents of the file", await Page.EvaluateAsync<string>(@"e => {
                var reader = new FileReader();
                var promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(e.files[0]);
                return promise.then(() => reader.result);
            }", input));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<input type=file>");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload);
            await Page.SetInputFilesAsync("input", filePath);

            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should set from memory")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetFromMemory()
        {
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", new FilePayload
            {
                Name = "test.txt",
                MimeType = "text/plain",
                Buffer = Encoding.UTF8.GetBytes("this is a test"),
            });

            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
            Assert.Equal("test.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit event once")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
        [Fact(Skip = "We dont'need to test this")]
        public void ShouldEmitEventOnOff()
        {
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit addListener/removeListener")]
        [Fact(Skip = "We dont'need to test this")]
        public void ShouldEmitEventAddListenerRemoveListener()
        {
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work when file input is attached to DOM")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenFileInputIsAttachedToDOM()
        {
            await Page.SetContentAsync("<input type=file>");
            var chooser = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.FileChooser),
                Page.ClickAsync("input")
            );
            Assert.NotNull(chooser?.Element);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work when file input is not attached to DOM")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenFileInputIsNotAttachedToDOM()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.FileChooser),
                Page.EvaluateAsync(@"() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }")
            );
            Assert.NotNull(chooser?.Element);
        }


        [PlaywrightTest("page-set-input-files.spec.ts", "should work with CSP")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "default-src \"none\"");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload));
            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldRespectTimeout() => Assert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForEventAsync(PageEvent.FileChooser, timeout: 1));

        [PlaywrightTest("page-set-input-files.spec.ts", "should respect default timeout when there is no custom timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout()
        {
            Page.SetDefaultTimeout(1);
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEventAsync(PageEvent.FileChooser));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should prioritize exact timeout over default timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPrioritizeExactTimeoutOverDefaultTimeout()
        {
            Page.SetDefaultTimeout(0);
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEventAsync(PageEvent.FileChooser, timeout: 1));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work with no timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.FileChooser, timeout: 0),
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }, 50)")
            );
            Assert.NotNull(chooser?.Element);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should return the same file chooser when there are many watchdogs simultaneously")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheSameFileChooserWhenThereAreManyWatchdogsSimultaneously()
        {
            await Page.SetContentAsync("<input type=file>");
            var (fileChooser1, fileChooser2, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.FileChooser),
                Page.WaitForEventAsync(PageEvent.FileChooser),
                Page.EvalOnSelectorAsync("input", "input => input.click()")
            );
            Assert.Equal(fileChooser1, fileChooser2);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should accept single file")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptSingleFile()
        {
            await Page.SetContentAsync("<input type=file oninput='javascript:console.timeStamp()'>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEventAsync(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );

            Assert.Same(Page, fileChooser.Page);
            Assert.NotNull(fileChooser.Element);
            await fileChooser.SetFilesAsync(TestConstants.FileToUpload);
            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should detect mime type")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

            await Page.GoToAsync(TestConstants.EmptyPage);
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

            Assert.Equal("file-to-upload.txt", files[0].name);
            Assert.Equal("text/plain", files[0].mime);
            Assert.Equal(File.ReadAllBytes(TestUtils.GetWebServerFile("file-to-upload.txt")), files[0].content);

            Assert.Equal("pptr.png", files[1].name);
            Assert.Equal("image/png", files[1].mime);
            Assert.Equal(File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png")), files[1].content);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should be able to read selected file")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToReadSelectedFile()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForEventAsync(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.SetFilesAsync(TestConstants.FileToUpload));
            Assert.Equal("contents of the file", await Page.EvalOnSelectorAsync<string>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                const reader = new FileReader();
                const promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(picker.files[0]);
                return promise.then(() => reader.result);
            }"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should be able to reset selected files with empty file list")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToResetSelectedFilesWithEmptyFileList()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForEventAsync(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.SetFilesAsync(TestConstants.FileToUpload));
            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
            _ = Page.WaitForEventAsync(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync(new string[] { }));
            Assert.Equal(0, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should not accept multiple files for single-file input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAcceptMultipleFilesForSingleFileInput()
        {
            await Page.SetContentAsync("<input type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEventAsync(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            await Assert.ThrowsAsync<PlaywrightException>(() => fileChooser.SetFilesAsync(new string[]
            {
                TestUtils.GetWebServerFile(TestConstants.FileToUpload),
                TestUtils.GetWebServerFile("pptr.png"),
            }));
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should emit input and change events")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(2, events.Count);
            Assert.Equal("input", events[0]);
            Assert.Equal("change", events[1]);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", "should work for single file pick")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForSingleFilePick()
        {
            await Page.SetContentAsync("<input type=file>");
            var waitTask = Page.WaitForEventAsync(PageEvent.FileChooser);

            var fileChooser = await TaskUtils.WhenAll(
               waitTask,
               Page.ClickAsync("input")
            );
            Assert.False(fileChooser.IsMultiple);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", @"should work for ""multiple""")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMultiple()
        {
            await Page.SetContentAsync("<input multiple type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEventAsync(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.IsMultiple);
        }

        [PlaywrightTest("page-set-input-files.spec.ts", @"should work for ""webkitdirectory""")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForWebkitdirectory()
        {
            await Page.SetContentAsync("<input multiple webkitdirectory type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEventAsync(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.IsMultiple);
        }
    }
}
