using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Input
{
    ///<playwright-file>input.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
    [Trait("Category", "chromium")]
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
        [Fact]
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
        [Fact]
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
        [Fact]
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
        [Fact]
        public Task ShouldRespectTimeout() => Assert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForEvent(PageEvent.FileChooser, new WaitForEventOptions<FileChooserEventArgs> { Timeout = 1 }));

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should respect default timeout when there is no custom timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectDefaultTimeoutWhenThereIsNoCustomTimeout()
        {
            Page.DefaultTimeout = 1;
            await Assert.ThrowsAsync<TimeoutException>(() => Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should prioritize exact timeout over default timeout</playwright-it>
        [Fact]
        public async Task ShouldPrioritizeExactTimeoutOverDefaultTimeout()
        {
            Page.DefaultTimeout = 0;
            await Assert.ThrowsAsync<TimeoutException>(()
                => Page.WaitForEvent(PageEvent.FileChooser, new WaitForEventOptions<FileChooserEventArgs> { Timeout = 1 }));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should work with no timeout</playwright-it>
        [Fact]
        public async Task ShouldWorkWithNoTimeout()
        {
            var (chooser, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.FileChooser, new WaitForEventOptions<FileChooserEventArgs> { Timeout = 0 }),
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
        [Fact]
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
        [Fact]
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
        ///<playwright-it>should be able to read selected file</playwright-it>
        [Fact]
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
        [Fact]
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
                .ContinueWith(task => task.Result.Element.SetInputFilesAsync());
            Assert.Equal(0, await Page.QuerySelectorEvaluateAsync<int>("input", @"async picker => {
                picker.click();
                await new Promise(x => picker.oninput = x);
                return picker.files.length;
            }"));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser</playwright-describe>
        ///<playwright-it>should not accept multiple files for single-file input</playwright-it>
        [Fact]
        public async Task ShouldNotAcceptMultipleFilesForSingleFileInput()
        {
            await Page.SetContentAsync("<input type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fileChooser.Element.SetInputFilesAsync(
                TestUtils.GetWebServerFile(TestConstants.FileToUpload),
                TestUtils.GetWebServerFile("pptr.png"))
            );
        }
    }
}
