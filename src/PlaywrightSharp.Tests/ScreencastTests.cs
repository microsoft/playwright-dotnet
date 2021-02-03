using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("screencast.spec.js", "videoSize should require videosPath")]
        [Fact(Skip = "We are not using old properties")]
        public void VideoSizeShouldRequireVideosPath()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should work with old options")]
        [Fact(Skip = "We are not using old properties")]
        public void ShouldWorkWithOldOptions()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should throw without recordVideo.dir")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowWithoutRecordVideoDir()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should capture static page")]
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

        [PlaywrightTest("screencast.spec.js", "should expose video path")]
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

        [PlaywrightTest("screencast.spec.js", "should expose video path blank page")]
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

        [PlaywrightTest("screencast.spec.js", "should expose video path blank popup")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldExposeVideoPathBlankPopup()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should capture navigation")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldCaptureNavigation()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should capture css transformation")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldCaptureCssTransformation()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should work for popups")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldWorkForPopups()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should scale frames down to the requested size")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldScaleFramesDownToTheRequestedSize()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should use viewport as default size")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldUseViewportAsDefaultSize()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should be 1280x720 by default")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldBe1280x720ByDefault()
        {
        }

        [PlaywrightTest("screencast.spec.js", "should capture static page in persistent context")]
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
