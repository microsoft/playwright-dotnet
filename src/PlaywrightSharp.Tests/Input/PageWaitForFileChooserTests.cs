using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Input
{
    ///<playwright-file>input.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForFileChooserTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForFileChooserTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should emit event</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitEvent()
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

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should work when file input is attached to DOM</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenFileInputIsAttachedToDOM()
        {
            await Page.SetContentAsync("<input type=file>");
            var chooser = await TaskUtils.WhenAll(
                Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
                Page.ClickAsync("input")
            );
            Assert.NotNull(chooser?.Element);
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should work when file input is not attached to DOM</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenFileInputIsNotAttachedToDOM()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
                Page.EvaluateAsync(@"() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }")
            );
            Assert.NotNull(chooser?.Element);
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldRespectTimeout() => Assert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser, timeout: 1));

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should respect default timeout when there is no custom timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout()
        {
            Page.DefaultTimeout = 1;
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should prioritize exact timeout over default timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPrioritizeExactTimeoutOverDefaultTimeout()
        {
            Page.DefaultTimeout = 0;
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser, timeout: 1));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should work with no timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser, timeout: 0),
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    var el = document.createElement('input');
                    el.type = 'file';
                    el.click();
                }, 50)")
            );
            Assert.NotNull(chooser?.Element);
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should return the same file chooser when there are many watchdogs simultaneously</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnTheSameFileChooserWhenThereAreManyWatchdogsSimultaneously()
        {
            await Page.SetContentAsync("<input type=file>");
            var (fileChooser1, fileChooser2) = await TaskUtils.WhenAll(
                Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
                Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
                Page.QuerySelectorEvaluateAsync("input", "input => input.click()")
            );
            Assert.Equal(fileChooser1, fileChooser2);
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should accept single file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAcceptSingleFile()
        {
            await Page.SetContentAsync("<input type=file oninput='javascript:console.timeStamp()'>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );

            await fileChooser.Element.SetInputFilesAsync(TestConstants.FileToUpload);
            Assert.Equal(1, await Page.QuerySelectorEvaluateAsync<int>("input", "input => input.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.QuerySelectorEvaluateAsync<string>("input", "input => input.files[0].name"));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
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

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should be able to read selected file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToReadSelectedFile()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync(TestConstants.FileToUpload));
            Assert.Equal("contents of the file", await Page.QuerySelectorEvaluateAsync<string>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                const reader = new FileReader();
                const promise = new Promise(fulfill => reader.onload = fulfill);
                reader.readAsText(picker.files[0]);
                return promise.then(() => reader.result);
            }"));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should be able to reset selected files with empty file list</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToResetSelectedFilesWithEmptyFileList()
        {
            await Page.SetContentAsync("<input type=file>");
            _ = Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync(TestConstants.FileToUpload));
            Assert.Equal(1, await Page.QuerySelectorEvaluateAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
            _ = Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser)
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync(new string[] { }));
            Assert.Equal(0, await Page.QuerySelectorEvaluateAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should not accept multiple files for single-file input</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotAcceptMultipleFilesForSingleFileInput()
        {
            await Page.SetContentAsync("<input type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            await Assert.ThrowsAsync<PlaywrightSharpException>(() => fileChooser.Element.SetInputFilesAsync(new string[]
            {
                TestUtils.GetWebServerFile(TestConstants.FileToUpload),
                TestUtils.GetWebServerFile("pptr.png"),
            }));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
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
    }
}
