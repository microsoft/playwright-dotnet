using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>screencast.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ScreencastTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public ScreencastTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("screencast.spec.ts", "videoSize should require videosPath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task VideoSizeShouldRequireVideosPath()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoSize = new RecordVideoSize { Height = 100, Width = 100 }
            }));

            Assert.Contains("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified", exception.Message);
        }

        [PlaywrightTest("screencast.spec.ts", "should work with old options")]
        [Fact(Skip = "We are not using old properties")]
        public void ShouldWorkWithOldOptions()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should throw without recordVideo.dir")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowWithoutRecordVideoDir()
        {
        }

        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task ShouldWorkWithoutASize()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoDir = tempDirectory.Path
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.NotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }

        [PlaywrightTest("screencast.spec.ts", "should capture static page")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task ShouldCaptureStaticPage()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize() { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.NotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }

        [PlaywrightTest("screencast.spec.ts", "should expose video path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeVideoPath()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            string path = await page.Video.PathAsync();
            Assert.Contains(tempDirectory.Path, path);
            await context.CloseAsync();

            Assert.True(new FileInfo(path).Exists);
        }

        [PlaywrightTest("screencast.spec.ts", "should expose video path blank page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeVideoPathBlankPage()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize() { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            string path = await page.Video.PathAsync();
            Assert.Contains(tempDirectory.Path, path);
            await context.CloseAsync();

            Assert.True(new FileInfo(path).Exists);
        }

        [PlaywrightTest("screencast.spec.ts", "should expose video path blank popup")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldExposeVideoPathBlankPopup()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should capture navigation")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldCaptureNavigation()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should capture css transformation")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldCaptureCssTransformation()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should work for popups")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldWorkForPopups()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should scale frames down to the requested size")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldScaleFramesDownToTheRequestedSize()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should use viewport as default size")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldUseViewportAsDefaultSize()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should be 1280x720 by default")]
        [Fact(Skip = "We don't need to test video details")]
        public void ShouldBe1280x720ByDefault()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should capture static page in persistent context")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task ShouldCaptureStaticPageInPersistentContext()
        {
            using var userDirectory = new TempDirectory();
            using var tempDirectory = new TempDirectory();
            var context = await BrowserType.LaunchPersistentContextAsync(userDirectory.Path, new BrowserTypeLaunchPersistentContextOptions
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize() { Height = 100, Width = 100 },
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.NotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }
    }
}
