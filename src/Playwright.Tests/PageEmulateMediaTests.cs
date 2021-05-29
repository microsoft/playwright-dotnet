using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEmulateMediaTests : PageTestEx
    {
        [PlaywrightTest("page-emulate-media.spec.ts", "should emulate scheme work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmulateSchemeWork()
        {
            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should default to light")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDefaultToLight()
        {
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));

            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Null });
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should throw in case of bad media argument")]
        [Test, Ignore("We don't need this test. Leaving for tracking purposes")]
        public void ShouldThrowInCaseOfBadMediaArgument() { }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work during navigation")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkDuringNavigation()
        {
            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
            var navigated = Page.GotoAsync(Server.EmptyPage);

            for (int i = 0; i < 9; i++)
            {
                await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = i % 2 == 0 ? ColorScheme.Dark : ColorScheme.Light });
                await Task.Delay(1);
            }
            await navigated;

            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work in popup")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkInPopup()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ColorScheme = ColorScheme.Dark,
            }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                var popupTask = page.WaitForPopupAsync();

                await TaskUtils.WhenAll(
                    popupTask,
                    page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

                var popup = popupTask.Result;

                Assert.True(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
                Assert.False(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ColorScheme = ColorScheme.Light,
            }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                var popupTask = page.WaitForPopupAsync();

                await TaskUtils.WhenAll(
                    popupTask,
                    page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

                var popup = popupTask.Result;

                Assert.False(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
                Assert.True(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            }
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work in cross-process iframe")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkInCrossProcessIframe()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ColorScheme = ColorScheme.Dark,
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(page, "frame1", Server.CrossProcessPrefix + "/empty.html");
            var frame = page.Frames.ElementAt(1);

            Assert.True(await frame.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should emulate type")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmulateType()
        {
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync();
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Null });
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should throw in case of bad colorScheme argument")]
        [Test, Ignore("We don't need this test. Leaving for tracking purposes")]
        public void ShouldThrowInCaseOfBadColorSchemeArgument() { }
    }
}
