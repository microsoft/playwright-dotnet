using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>page-set-input-files.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetInputFilesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetInputFilesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should upload the file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<input type=file>");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload);
            await Page.SetInputFilesAsync("input", filePath);

            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should set from memory</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSetFromMemory()
        {
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", new FilePayload
            {
                Name = "test.txt",
                MimeType = "text/plain",
                Buffer = Convert.ToBase64String(Encoding.UTF8.GetBytes("this is a test"))
            });

            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "e => e.files.length"));
            Assert.Equal("test.txt", await Page.EvalOnSelectorAsync<string>("input", "e => e.files[0].name"));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should emit event once</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitEventOnce()
        {
            await Page.SetContentAsync("<input type=file>");
            var chooserTsc = new TaskCompletionSource<IElementHandle>();
            void EventHandler(object sender, FileChooserEventArgs e)
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

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should emit event on/off</playwright-it>
        [Fact(Skip = "We dont'need to test this")]
        public void ShouldEmitEventOnOff()
        {
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should emit addListener/removeListener</playwright-it>
        [Fact(Skip = "We dont'need to test this")]
        public void ShouldEmitEventAddListenerRemoveListener()
        {
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work when file input is attached to DOM</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenFileInputIsAttachedToDOM()
        {
            await Page.SetContentAsync("<input type=file>");
            var chooser = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.FileChooser),
                Page.ClickAsync("input")
            );
            Assert.NotNull(chooser?.Element);
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work when file input is not attached to DOM</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenFileInputIsNotAttachedToDOM()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.FileChooser),
                Page.EvaluateAsync(@"() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }")
            );
            Assert.NotNull(chooser?.Element);
        }


        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work with CSP</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "default-src \"none\"");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload));
            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldRespectTimeout() => Assert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForEvent(PageEvent.FileChooser, timeout: 1));

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should respect default timeout when there is no custom timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout()
        {
            Page.DefaultTimeout = 1;
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEvent(PageEvent.FileChooser));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should prioritize exact timeout over default timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPrioritizeExactTimeoutOverDefaultTimeout()
        {
            Page.DefaultTimeout = 0;
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEvent(PageEvent.FileChooser, timeout: 1));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work with no timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.FileChooser, timeout: 0),
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }, 50)")
            );
            Assert.NotNull(chooser?.Element);
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should return the same file chooser when there are many watchdogs simultaneously</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnTheSameFileChooserWhenThereAreManyWatchdogsSimultaneously()
        {
            await Page.SetContentAsync("<input type=file>");
            var (fileChooser1, fileChooser2) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.FileChooser),
                Page.WaitForEvent(PageEvent.FileChooser),
                Page.EvalOnSelectorAsync("input", "input => input.click()")
            );
            Assert.Equal(fileChooser1, fileChooser2);
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should accept single file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAcceptSingleFile()
        {
            await Page.SetContentAsync("<input type=file oninput='javascript:console.timeStamp()'>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );

            Assert.Same(Page, fileChooser.Page);
            Assert.NotNull(fileChooser.Element);
            await fileChooser.SetFilesAsync(TestConstants.FileToUpload);
            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.EvalOnSelectorAsync<string>("input", "input => input.files[0].name"));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>

        ///<playwright-it>should detect mime type</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should be able to read selected file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToReadSelectedFile()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForEvent(PageEvent.FileChooser)
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

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should be able to reset selected files with empty file list</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToResetSelectedFilesWithEmptyFileList()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForEvent(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.SetFilesAsync(TestConstants.FileToUpload));
            Assert.Equal(1, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
            _ = Page.WaitForEvent(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync(new string[] { }));
            Assert.Equal(0, await Page.EvalOnSelectorAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should not accept multiple files for single-file input</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotAcceptMultipleFilesForSingleFileInput()
        {
            await Page.SetContentAsync("<input type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            await Assert.ThrowsAsync<PlaywrightSharpException>(() => fileChooser.SetFilesAsync(new string[]
            {
                TestUtils.GetWebServerFile(TestConstants.FileToUpload),
                TestUtils.GetWebServerFile("pptr.png"),
            }));
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should emit input and change events</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work for single file pick</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForSingleFilePick()
        {
            await Page.SetContentAsync("<input type=file>");
            var waitTask = Page.WaitForEvent(PageEvent.FileChooser);

            var fileChooser = await TaskUtils.WhenAll(
               waitTask,
               Page.ClickAsync("input")
            );
            Assert.False(fileChooser.IsMultiple);
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work for "multiple"</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForMultiple()
        {
            await Page.SetContentAsync("<input multiple type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.IsMultiple);
        }

        ///<playwright-file>page-set-input-files.spec.js</playwright-file>
        ///<playwright-it>should work for "webkitdirectory"</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForWebkitdirectory()
        {
            await Page.SetContentAsync("<input multiple webkitdirectory type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.IsMultiple);
        }
    }
}
