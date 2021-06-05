using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>screencast.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class ScreencastTests : BrowserTestEx
    {
        [PlaywrightTest("screencast.spec.ts", "videoSize should require videosPath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task VideoSizeShouldRequireVideosPath()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new()
            {
                RecordVideoSize = new RecordVideoSize { Height = 100, Width = 100 }
            }));

            StringAssert.Contains("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified", exception.Message);
        }

        [PlaywrightTest("screencast.spec.ts", "should work with old options")]
        [Test, Ignore("We are not using old properties")]
        public void ShouldWorkWithOldOptions()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should throw without recordVideo.dir")]
        [Test, Ignore("We don't need to test this")]
        public void ShouldThrowWithoutRecordVideoDir()
        {
        }

        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithoutASize()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new()
            {
                RecordVideoDir = tempDirectory.Path
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.IsNotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }

        [PlaywrightTest("screencast.spec.ts", "should capture static page")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipWindows: true)]
        public async Task ShouldCaptureStaticPage()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new()
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize() { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.IsNotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }

        [PlaywrightTest("screencast.spec.ts", "should expose video path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeVideoPath()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new()
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            string path = await page.Video.PathAsync();
            StringAssert.Contains(tempDirectory.Path, path);
            await context.CloseAsync();

            Assert.True(new FileInfo(path).Exists);
        }

        [PlaywrightTest("screencast.spec.ts", "should expose video path blank page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeVideoPathBlankPage()
        {
            using var tempDirectory = new TempDirectory();
            var context = await Browser.NewContextAsync(new()
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize() { Height = 100, Width = 100 }
            });

            var page = await context.NewPageAsync();
            string path = await page.Video.PathAsync();
            StringAssert.Contains(tempDirectory.Path, path);
            await context.CloseAsync();

            Assert.True(new FileInfo(path).Exists);
        }

        [PlaywrightTest("screencast.spec.ts", "should expose video path blank popup")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldExposeVideoPathBlankPopup()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should capture navigation")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldCaptureNavigation()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should capture css transformation")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldCaptureCssTransformation()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should work for popups")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldWorkForPopups()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should scale frames down to the requested size")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldScaleFramesDownToTheRequestedSize()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should use viewport as default size")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldUseViewportAsDefaultSize()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should be 1280x720 by default")]
        [Test, Ignore("We don't need to test video details")]
        public void ShouldBe1280x720ByDefault()
        {
        }

        [PlaywrightTest("screencast.spec.ts", "should capture static page in persistent context")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipFirefox: true)]
        public async Task ShouldCaptureStaticPageInPersistentContext()
        {
            using var userDirectory = new TempDirectory();
            using var tempDirectory = new TempDirectory();
            var context = await BrowserType.LaunchPersistentContextAsync(userDirectory.Path, new()
            {
                RecordVideoDir = tempDirectory.Path,
                RecordVideoSize = new RecordVideoSize() { Height = 100, Width = 100 },
            });

            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await Task.Delay(1000);
            await context.CloseAsync();

            Assert.IsNotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
        }
    }
}
