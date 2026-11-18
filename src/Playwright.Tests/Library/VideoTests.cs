/**
 * Copyright Microsoft Corporation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Runtime.InteropServices;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

public abstract class VideoTestsBase : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Video == "on", "conflicts with built-in video recording");
}

/// <summary>Port of tests/library/video.spec.ts.</summary>
public class VideoTests : VideoTestsBase
{
    private sealed record Pixel(int r, int g, int b, int alpha);

    private static bool IsAlmostRed(Pixel pixel)
    {
        var (r, g, b, alpha) = pixel;
        return r > 185 && g < 70 && b < 70 && alpha == 255;
    }

    private static bool IsAlmostBlack(Pixel pixel)
    {
        var (r, g, b, alpha) = pixel;
        return r < 70 && g < 70 && b < 70 && alpha == 255;
    }

    private static bool IsAlmostGray(Pixel pixel)
    {
        var (r, g, b, alpha) = pixel;
        return r > 70 && r < 185 &&
          g > 70 && g < 185 &&
          b > 70 && b < 185 &&
          alpha == 255;
    }

    private static Pixel FindPixel(byte[] pixels, Func<Pixel, bool> pixelPredicate)
    {
        for (int i = 0, n = pixels.Length; i < n; i += 4)
        {
            var pixel = new Pixel(
              r: pixels[i],
              g: pixels[i + 1],
              b: pixels[i + 2],
              alpha: pixels[i + 3]);
            if (pixelPredicate(pixel))
            {
                return pixel;
            }
        }
        return null;
    }

    private static bool EveryPixel(byte[] pixels, Func<Pixel, bool> pixelPredicate)
    {
        var badPixel = FindPixel(pixels, pixel => !pixelPredicate(pixel));
        return badPixel == null;
    }

    private static void ExpectAll(byte[] pixels, Func<Pixel, bool> pixelPredicate)
    {
        var badPixel = FindPixel(pixels, pixel => !pixelPredicate(pixel));
        if (badPixel == null)
        {
            return;
        }
        var rgba = string.Join(", ", new[] { badPixel.r, badPixel.g, badPixel.b, badPixel.alpha });
        throw new Exception(string.Join("\n", new[]
        {
            $"Expected all pixels to satisfy {pixelPredicate.Method.Name}, found bad pixel ({rgba})",
            $"Actual pixels=[{string.Join(",", pixels)}]",
        }));
    }

    private static string[] FindVideos(string videoDir)
    {
        var files = Directory.GetFileSystemEntries(videoDir).Select(Path.GetFileName);
        return files.Where(file => file.EndsWith("webm", StringComparison.Ordinal)).Select(file => Path.Combine(videoDir, file)).ToArray();
    }

    private static void ExpectRedFrames(string videoFile, (int width, int height) size)
    {
        ExpectFrames(videoFile, size, IsAlmostRed);
    }

    private static void ExpectFrames(string videoFile, (int width, int height) size, Func<Pixel, bool> pixelPredicate)
    {
        var videoPlayer = new VideoPlayer(videoFile);
        var duration = videoPlayer.Duration;
        Assert.That(duration, Is.GreaterThan(0));

        Assert.That(videoPlayer.VideoWidth, Is.EqualTo(size.width));
        Assert.That(videoPlayer.VideoHeight, Is.EqualTo(size.height));

        {
            var pixels = videoPlayer.SeekLastFrame().Data;
            ExpectAll(pixels, pixelPredicate);
        }
        {
            var pixels = videoPlayer.SeekLastFrame((x: size.width - 20, y: 10)).Data;
            ExpectAll(pixels, pixelPredicate);
        }
    }

    public class Screencast : VideoTestsBase
    {
        [SetUp]
        public void Slow() => Info.Slow();

        [SetUp]
        public void Skip2() => Info.Skip(Mode != "default", "video.path() is not available in remote mode");

        [UpstreamTest("VI-001 should not have video by default")]
        public async Task ShouldNotHaveVideoByDefault()
        {
            var page = await PageFixture();
            Assert.That(page.Video, Is.Null);
        }

        [UpstreamTest("VI-003 should capture static page")]
        public async Task ShouldCaptureStaticPage()
        {
            // Firefox does not have a mobile variant and has a large minimum size (500 on windows and 450 elsewhere).
            var size = BrowserName == "firefox" ? (width: 500, height: 400) : (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();

            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            ExpectRedFrames(videoFile, size);
        }

        [UpstreamTest("VI-004 should pad a short frame to the video size")]
        public async Task ShouldPadAShortFrameToTheVideoSize()
        {
            var videoSize = (width: 800, height: 600);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = videoSize.width, Height = videoSize.height },
                ViewportSize = new() { Width = 800, Height = 396 },
            });
            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(videoSize.width));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(videoSize.height));
        }

        [UpstreamTest("VI-005 should continue recording main page after popup closes")]
        public async Task ShouldContinueRecordingMainPageAfterPopupCloses()
        {
            // Firefox does not have a mobile variant and has a large minimum size (500 on windows and 450 elsewhere).
            var size = BrowserName == "firefox" ? (width: 500, height: 400) : (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync("<a target=_blank href=\"about:blank\">clickme</a>");
            // .NET: the second item of the array is 'await page.click('a')', so the click ends before Promise.all starts.
            var popupPromise = page.WaitForPopupAsync();
            await page.ClickAsync("a");
            var popup = await popupPromise;
            await popup.CloseAsync();

            await page.EvaluateAsync(@"() => {
      document.body.textContent = ''; // remove link
      document.body.style.backgroundColor = 'red';
    }");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            ExpectRedFrames(videoFile, size);
        }

        [UpstreamTest("VI-006 should expose video path")]
        public async Task ShouldExposeVideoPath()
        {
            var videosPath = Info.OutputPath("");
            var size = (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            var path = await page.Video.PathAsync();
            Assert.That(path, Does.Contain(videosPath));
            await context.CloseAsync();
            Assert.That(File.Exists(path), Is.True);
        }

        [UpstreamTest("VI-007 should delete video")]
        public async Task ShouldDeleteVideo()
        {
            var videosPath = Info.OutputPath("");
            var size = (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            var deletePromise = page.Video.DeleteAsync();
            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoPath = await page.Video.PathAsync();
            await deletePromise;
            Assert.That(File.Exists(videoPath), Is.False);
        }

        [UpstreamTest("VI-008 should expose video path blank page")]
        public async Task ShouldExposeVideoPathBlankPage()
        {
            var videosPath = Info.OutputPath("");
            var size = (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            var path = await page.Video.PathAsync();
            Assert.That(path, Does.Contain(videosPath));
            await context.CloseAsync();
            Assert.That(File.Exists(path), Is.True);
        }

        [UpstreamTest("VI-009 should work with weird screen resolution")]
        public async Task ShouldWorkWithWeirdScreenResolution()
        {
            var videosPath = Info.OutputPath("");
            var size = (width: 1904, height: 609);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            var path = await page.Video.PathAsync();
            Assert.That(path, Does.Contain(videosPath));
            await context.CloseAsync();
            Assert.That(File.Exists(path), Is.True);
        }

        [UpstreamTest("VI-010 should work with relative path for recordVideo.dir")]
        public async Task ShouldWorkWithRelativePathForRecordVideoDir()
        {
            var videosPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Info.OutputPath(""));
            var size = (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            var videoPath = await page.Video.PathAsync();
            await context.CloseAsync();
            Assert.That(File.Exists(videoPath), Is.True);
        }

        [UpstreamTest("VI-011 should expose video path blank popup")]
        public async Task ShouldExposeVideoPathBlankPopup()
        {
            var videosPath = Info.OutputPath("");
            var size = (width: 320, height: 240);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("window.open(\"about:blank\")"));
            var path = await popup.Video.PathAsync();
            Assert.That(path, Does.Contain(videosPath));
            await context.CloseAsync();
            Assert.That(File.Exists(path), Is.True);
        }

        [UpstreamTest("VI-012 should capture navigation")]
        public async Task ShouldCaptureNavigation()
        {
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = 1280, Height = 720 },
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/background-color.html#rgb(0,0,0)");
            await EnsureSomeFrames(page);
            await page.GotoAsync(Server.CrossProcessPrefix + "/background-color.html#rgb(100,100,100)");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            var duration = videoPlayer.Duration;
            Assert.That(duration, Is.GreaterThan(0));

            {
                // Find a frame with all almost-black pixels.
                var frame = videoPlayer.FindFrame(pixels => EveryPixel(pixels, IsAlmostBlack));
                Assert.That(frame, Is.Not.Null);
            }

            {
                var pixels = videoPlayer.SeekLastFrame().Data;
                ExpectAll(pixels, IsAlmostGray);
            }
        }

        [UpstreamTest("VI-013 should capture css transformation")]
        public async Task ShouldCaptureCssTransformation()
        {
            Info.Fixme(!Headless, "Fails on headed");
            Info.Fixme(BrowserName == "webkit" && Platform == "win32");

            var size = (width: 600, height: 400);
            // Set viewport equal to screencast frame size to avoid scaling.
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/rotate-z.html");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            var duration = videoPlayer.Duration;
            Assert.That(duration, Is.GreaterThan(0));

            {
                var pixels = videoPlayer.SeekLastFrame((x: 95, y: 45)).Data;
                ExpectAll(pixels, IsAlmostRed);
            }
        }

        [UpstreamTest("VI-014 should work for popups")]
        public async Task ShouldWorkForPopups()
        {
            Info.Fixme(BrowserName == "firefox" && !IsBidi, "https://github.com/microsoft/playwright/issues/14557");
            var videosPath = Info.OutputPath("");
            var size = (width: 600, height: 400);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videosPath,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("() => { window.open('about:blank'); }"));
            await popup.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await PromiseAll(
                EnsureSomeFrames(page),
                EnsureSomeFrames(popup));
            await context.CloseAsync();

            var pageVideoFile = await page.Video.PathAsync();
            var popupVideoFile = await popup.Video.PathAsync();
            Assert.That(pageVideoFile, Is.Not.EqualTo(popupVideoFile));
            ExpectRedFrames(popupVideoFile, size);

            var videoFiles = FindVideos(videosPath);
            Assert.That(videoFiles.Length, Is.EqualTo(2));
        }

        [UpstreamTest("VI-015 should scale frames down to the requested size ")]
        public async Task ShouldScaleFramesDownToTheRequestedSize()
        {
            Info.Fixme(!Headless, "Fails on headed");
            Info.Fixme(BrowserName == "chromium" && !IsHeadlessShell, "Chromium (but not headless shell) has a min width issue");

            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                // Set size to 1/2 of the viewport.
                RecordVideoSize = new() { Width = 320, Height = 240 },
                ViewportSize = new() { Width = 640, Height = 480 },
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/checkerboard.html");
            // Update the picture to ensure enough frames are generated.
            await page.EvalOnSelectorAsync(".container", @"container => {
      container.firstElementChild.classList.remove('red');
    }");
            await EnsureSomeFrames(page);
            await page.EvalOnSelectorAsync(".container", @"container => {
      container.firstElementChild.classList.add('red');
    }");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            var duration = videoPlayer.Duration;
            Assert.That(duration, Is.GreaterThan(0));

            {
                var pixels = videoPlayer.SeekLastFrame((x: 10, y: 10)).Data;
                ExpectAll(pixels, IsAlmostRed);
            }
            {
                var pixels = videoPlayer.SeekLastFrame((x: 300, y: 10)).Data;
                ExpectAll(pixels, IsAlmostGray);
            }
            {
                var pixels = videoPlayer.SeekLastFrame((x: 10, y: 200)).Data;
                ExpectAll(pixels, IsAlmostGray);
            }
            {
                var pixels = videoPlayer.SeekLastFrame((x: 300, y: 200)).Data;
                ExpectAll(pixels, IsAlmostRed);
            }
        }

        [UpstreamTest("VI-016 should use viewport scaled down to fit into 800x800 as default size")]
        public async Task ShouldUseViewportScaledDownToFitInto800x800AsDefaultSize()
        {
            var size = (width: 1600, height: 1200);
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                ViewportSize = new() { Width = size.width, Height = size.height },
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(800));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(600));
        }

        [UpstreamTest("VI-017 should be 800x450 by default")]
        public async Task ShouldBe800x450ByDefault()
        {
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(800));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(450));
        }

        [UpstreamTest("VI-018 should record video with the requested fps")]
        public async Task ShouldRecordVideoWithTheRequestedFps()
        {
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoFps = 60,
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoPlayer = new VideoPlayer(await page.Video.PathAsync());
            Assert.That(videoPlayer.Fps, Is.EqualTo(60));
        }

        [UpstreamTest("VI-019 should throw on invalid fps")]
        public async Task ShouldThrowOnInvalidFps()
        {
            var error = await Catch(() => Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoFps = 0,
            }));
            Assert.That(error.Message, Does.Contain("\"recordVideo.fps\" must be a positive number, got 0"));
        }

        [UpstreamTest("VI-020 should be 800x600 with null viewport")]
        public async Task ShouldBe800x600WithNullViewport()
        {
            Info.Fixme(BrowserName == "firefox" && Headless && !IsBidi, "Fails in headless on bots");

            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                ViewportSize = ViewportSize.NoViewport,
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(800));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(600));
        }

        [UpstreamTest("VI-021 should capture static page in persistent context @smoke")]
        public async Task ShouldCaptureStaticPageInPersistentContext()
        {
            Info.Skip(BrowserName == "webkit" && IsMac && RuntimeInformation.ProcessArchitecture == Architecture.Arm64, "Is only failing on self-hosted github actions runner on M1 mac; not reproducible locally");
            var size = (width: 600, height: 400);
            var (context, page) = await LaunchPersistent(new()
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });

            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            var duration = videoPlayer.Duration;
            Assert.That(duration, Is.GreaterThan(0));

            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(600));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(400));

            {
                var pixels = videoPlayer.SeekLastFrame().Data;
                ExpectAll(pixels, IsAlmostRed);
            }
        }

        [UpstreamTest("VI-022 should emulate an iphone")]
        public async Task ShouldEmulateAnIphone()
        {
            var device = Playwright.Devices["iPhone 6"];
            var context = await ContextFactory(new(device)
            {
                RecordVideoDir = Info.OutputPath(""),
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            var videoPlayer = new VideoPlayer(videoFile);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(374));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(666));
        }

        [UpstreamTest("VI-023 should throw on browser close")]
        public async Task ShouldThrowOnBrowserClose()
        {
            var size = (width: 320, height: 240);
            var browser = await BrowserType.LaunchAsync(LaunchOptions());
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await browser.CloseAsync();

            var file = Info.OutputPath("saved-video-");
            var saveResult = await Catch(() => page.Video.SaveAsAsync(file));
            Assert.That(saveResult.Message, Does.Contain("browser has been closed"));
        }

        [UpstreamTest("VI-025 should wait for video to finish if page was closed")]
        public async Task ShouldWaitForVideoToFinishIfPageWasClosed()
        {
            var size = (width: 320, height: 240);
            var browser = await BrowserType.LaunchAsync(LaunchOptions());

            var videoDir = Info.OutputPath("");
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videoDir,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });

            var page = await context.NewPageAsync();
            await EnsureSomeFrames(page);
            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();

            var videoFiles = FindVideos(videoDir);
            Assert.That(videoFiles.Length, Is.EqualTo(1));
            var videoPlayer = new VideoPlayer(videoFiles[0]);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(320));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(240));
        }

        [UpstreamTest("VI-026 should close ffmpeg even if there were no frames")]
        public async Task ShouldCloseFfmpegEvenIfThereWereNoFrames()
        {
            var size = (width: 320, height: 240);
            var browser = await BrowserType.LaunchAsync(LaunchOptions());

            var videoDir = Info.OutputPath("");
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videoDir,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });

            var page1 = await context.NewPageAsync();
            await page1.CloseAsync();

            var page2 = await context.NewPageAsync();
            await page2.CloseAsync();

            await context.CloseAsync();
            await browser.CloseAsync();

            var videoFiles = FindVideos(videoDir);
            Assert.That(videoFiles.Length, Is.EqualTo(2));
        }

        [UpstreamTest("VI-027 should not create video for internal pages")]
        public async Task ShouldNotCreateVideoForInternalPages()
        {
            Server.SetRoute("/empty.html", (req, res) =>
            {
                res.SetHeader("Set-Cookie", "name=value");
                res.End();
            });

            var videoDir = Info.OutputPath("");
            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = videoDir,
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await EnsureSomeFrames(page);

            var cookies = await context.CookiesAsync();
            Assert.That(cookies.Count, Is.EqualTo(1));
            await context.StorageStateAsync();
            await context.CloseAsync();

            var files = Directory.GetFileSystemEntries(videoDir);
            Assert.That(files.Length, Is.EqualTo(1));
        }

        [UpstreamTest("VI-028 should capture full viewport")]
        public async Task ShouldCaptureFullViewport()
        {
            Info.Fixme(BrowserName == "chromium" && !IsHeadlessShell, "The square is not on the video");
            Info.Fixme(BrowserName == "firefox" && IsWindows, "https://github.com/microsoft/playwright/issues/14405");
            var size = (width: 600, height: 400);
            var browser = await BrowserType.LaunchAsync(LaunchOptions());

            var videoDir = Info.OutputPath("");
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                ViewportSize = new() { Width = size.width, Height = size.height },
                RecordVideoDir = videoDir,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
            });

            var page = await context.NewPageAsync();
            await page.SetContentAsync("<div style='margin: 0; background: red; position: fixed; right:0; bottom:0; width: 30; height: 30;'></div>");
            await EnsureSomeFrames(page);
            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();

            var videoFiles = FindVideos(videoDir);
            Assert.That(videoFiles.Length, Is.EqualTo(1));
            var videoPlayer = new VideoPlayer(videoFiles[0]);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(size.width));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(size.height));

            // Bottom right corner should be part of the red border.
            // However, headed browsers on mac have rounded corners, so offset by 10.
            var pixels = videoPlayer.SeekLastFrame((x: size.width - 20, y: size.height - 20)).Data;
            ExpectAll(pixels, IsAlmostRed);
        }

        [UpstreamTest("VI-029 should capture full viewport on hidpi")]
        public async Task ShouldCaptureFullViewportOnHidpi()
        {
            Info.Fixme(BrowserName == "chromium" && !IsHeadlessShell, "The square is not on the video");
            Info.Fixme(BrowserName == "firefox" && IsWindows, "https://github.com/microsoft/playwright/issues/14405");
            Info.Fixme(BrowserName == "webkit" && !Headless && (IsLinux || (IsWindows && Channel == "webkit-wsl")), "https://github.com/microsoft/playwright/issues/22617");
            var size = (width: 600, height: 400);
            var browser = await BrowserType.LaunchAsync(LaunchOptions());

            var videoDir = Info.OutputPath("");
            var context = await browser.NewContextAsync(new(ContextOptions())
            {
                ViewportSize = new() { Width = size.width, Height = size.height },
                DeviceScaleFactor = 3,
                RecordVideoDir = videoDir,
                RecordVideoSize = new() { Width = size.width, Height = size.height },
            });

            var page = await context.NewPageAsync();
            await page.SetContentAsync("<div style='margin: 0; background: red; position: fixed; right:0; bottom:0; width: 30; height: 30;'></div>");
            await EnsureSomeFrames(page);
            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();

            var videoFiles = FindVideos(videoDir);
            Assert.That(videoFiles.Length, Is.EqualTo(1));
            var videoPlayer = new VideoPlayer(videoFiles[0]);
            Assert.That(videoPlayer.VideoWidth, Is.EqualTo(size.width));
            Assert.That(videoPlayer.VideoHeight, Is.EqualTo(size.height));

            // Bottom right corner should be part of the red border.
            // However, headed browsers on mac have rounded corners, so offset by 10.
            var pixels = videoPlayer.SeekLastFrame((x: size.width - 20, y: size.height - 20)).Data;
            ExpectAll(pixels, IsAlmostRed);
        }

        [UpstreamTest("VI-030 should work with video+trace")]
        public async Task ShouldWorkWithVideoTrace()
        {
            Info.Skip(Trace == "on");
            Info.Fixme(!Headless, "different trace screencast image size on all browsers");
            Info.Fixme(BrowserName == "chromium" && !IsHeadlessShell, "different trace screencast image size");

            var size = (width: 500, height: 400);
            var traceFile = Info.OutputPath("trace.zip");

            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                RecordVideoDir = Info.OutputPath(""),
                RecordVideoSize = new() { Width = size.width, Height = size.height },
                ViewportSize = new() { Width = size.width, Height = size.height },
            });
            await context.Tracing.StartAsync(new() { Screenshots = true });
            var page = await context.NewPageAsync();

            await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
            await EnsureSomeFrames(page);
            await context.Tracing.StopAsync(new() { Path = traceFile });
            await context.CloseAsync();

            var videoFile = await page.Video.PathAsync();
            ExpectRedFrames(videoFile, size);

            var (events, resources, _, _, _) = await ParseTraceRaw(traceFile);
            var frame = events.Where(e => e.GetProperty("type").GetString() == "screencast-frame").Last();
            var buffer = resources[frame.GetProperty("file").GetString()];
            // .NET: jpegjs.decode(buffer)
            var image = ImageComparator.Decode("image/jpeg", buffer);
            Assert.That(image.Width, Is.EqualTo(size.width));
            Assert.That(image.Height, Is.EqualTo(size.height));
            var offset = (size.width * size.height / 2 * 4) + (size.width * 4 / 2); // Center should be red.
            var pixel = new Pixel(
              r: image.Data[offset],
              g: image.Data[offset + 1],
              b: image.Data[offset + 2],
              alpha: image.Data[offset + 3]);
            Assert.That(IsAlmostRed(pixel), Is.EqualTo(true));
        }
    }

    [UpstreamTest("VI-031 should saveAs video")]
    public async Task ShouldSaveAsVideo()
    {
        Info.Slow();

        var videosPath = Info.OutputPath("");
        var size = (width: 320, height: 240);
        var context = await Browser.NewContextAsync(new(ContextOptions())
        {
            RecordVideoDir = videosPath,
            RecordVideoSize = new() { Width = size.width, Height = size.height },
            ViewportSize = new() { Width = size.width, Height = size.height },
        });
        var page = await context.NewPageAsync();
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await EnsureSomeFrames(page);
        await context.CloseAsync();

        var saveAsPath = Info.OutputPath("my-video.webm");
        await page.Video.SaveAsAsync(saveAsPath);
        Assert.That(File.Exists(saveAsPath), Is.True);
    }
}
