using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>screencast.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ScreencastTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public ScreencastTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>videoSize should require videosPath</playwright-it>
        [Fact(Skip = "We are not using old properties")]
        public void VideoSizeShouldRequireVideosPath()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should work with old options</playwright-it>
        [Fact(Skip = "We are not using old properties")]
        public void ShouldWorkWithOldOptions()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should throw without recordVideo.dir</playwright-it>
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowWithoutRecordVideoDir()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should capture static page</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task ShouldCaptureStaticPage()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(
                recordVideo: new RecordVideoOptions
                {
                    Dir = tempDirectory.Path,
                    Size = new ViewportSize { Width = 100, Height = 100 },
                });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.NotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should expose video path</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeVideoPath()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(
                recordVideo: new RecordVideoOptions
                {
                    Dir = tempDirectory.Path,
                    Size = new ViewportSize { Width = 100, Height = 100 }
                });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            string path = await page.Video.GetPathAsync();
            Assert.Contains(tempDirectory.Path, path);
            await context.CloseAsync();

            Assert.True(new FileInfo(path).Exists);
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should expose video path blank page</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeVideoPathBlankPage()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(
                recordVideo: new RecordVideoOptions
                {
                    Dir = tempDirectory.Path,
                    Size = new ViewportSize { Width = 100, Height = 100 }
                });

            var page = await context.NewPageAsync();
            string path = await page.Video.GetPathAsync();
            Assert.Contains(tempDirectory.Path, path);
            await context.CloseAsync();

            Assert.True(new FileInfo(path).Exists);
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should expose video path blank popup</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldExposeVideoPathBlankPopup()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should capture navigation</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldCaptureNavigation()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should capture css transformation</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldCaptureCssTransformation()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should work for popups</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldWorkForPopups()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should scale frames down to the requested size</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldScaleFramesDownToTheRequestedSize()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should use viewport as default size</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldUseViewportAsDefaultSize()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should be 1280x720 by default</playwright-it>
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldBe1280x720ByDefault()
        {
        }

        ///<playwright-file>screencast.spec.js</playwright-file>     
        ///<playwright-it>should capture static page in persistent context</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task ShouldCaptureStaticPageInPersistentContext()
        {
            using var userDirectory = new TempDirectory();
            using var tempDirectory = new TempDirectory();
            var context = await BrowserType.LaunchPersistentContextAsync(
                userDirectory.Path,
                recordVideo: new RecordVideoOptions
                {
                    Dir = tempDirectory.Path,
                    Size = new ViewportSize { Width = 100, Height = 100 }
                });


            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.NotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }
    }
}
