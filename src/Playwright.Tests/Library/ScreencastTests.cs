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

using System.Buffers.Binary;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/screencast.spec.ts.</summary>
public class ScreencastTests
{
    [SetUp]
    public void Slow() => Info.Slow();

    [BeforeEach]
    public static void Skip(string mode) => Info.Skip(mode != "default", "screencast is not available in remote mode");

    [BeforeEach]
    public static void Skip2(string video) => Info.Skip(video == "on", "conflicts with built-in video recording");

    [UpstreamTest("SC-001 screencast.start delivers frames via onFrame callback")]
    public async Task ScreencastStartDeliversFramesViaOnFrameCallback(IBrowser browser, Server server, string trace, string browserName, bool isMac, bool headless, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(trace == "on", "trace=on has different screencast image configuration");
        Info.Fixme(browserName == "firefox" && isMac && !headless, "wrong frame size in headed Firefox on Mac");

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 1000, Height = 400 } });
        var page = await context.NewPageAsync();

        var frames = new List<byte[]>();
        var size = new ScreencastSize { Width = 500, Height = 400 };
        await page.Screencast.StartAsync(new()
        {
            OnFrame = frame =>
            {
                frames.Add(frame.Data);
                return Task.CompletedTask;
            },
            Size = size,
        });
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();

        Assert.That(frames.Count, Is.GreaterThan(0));
        foreach (var frame in frames)
        {
            // Each frame must be a valid JPEG (starts with FF D8)
            Assert.That(frame[0], Is.EqualTo(0xff));
            Assert.That(frame[1], Is.EqualTo(0xd8));
        }
        // Chromium scales the first frame against the window size it had before the viewport
        // was applied, so only the last frame is reliably scaled down to fit the maximum size.
        var (width, height) = JpegDimensions(frames[frames.Count - 1]);
        Assert.That(width, Is.EqualTo(500));
        Assert.That(height, Is.EqualTo(200));

        await context.CloseAsync();
    }

    [UpstreamTest("SC-002 applies backpressure while async onFrame callback is pending")]
    [LocalFixme("dotnet: the client never sends screencastFrameAck after the OnFrame callback (Screencast.OnScreencastFrame), so the driver stops sending frames: no frame comes after the test releases the callback")]
    public async Task AppliesBackpressureWhileAsyncOnFrameCallbackIsPending(IBrowser browser, Server server, string trace, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(trace == "on", "trace recording acknowledges screencast frames independently");

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 400 } });
        var page = await context.NewPageAsync();

        var callbackDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstFrameReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var frameCount = 0;
        long lastFrameTimestamp = 0;
        await page.Screencast.StartAsync(new()
        {
            OnFrame = async _ =>
            {
                ++frameCount;
                lastFrameTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                firstFrameReceived.TrySetResult();
                await callbackDone.Task;
            },
        });
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    const animate = () => {
      document.body.style.backgroundColor = document.body.style.backgroundColor === 'red' ? 'blue' : 'red';
      requestAnimationFrame(animate);
    };
    requestAnimationFrame(animate);
  }");
        await firstFrameReceived.Task;
        await Expect(() => Assert.That(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastFrameTimestamp, Is.GreaterThan(1000))).ToPassAsync(new() { Timeout = 30000 });

        var framesWhileBlocked = frameCount;
        await EnsureSomeFrames(page);
        Assert.That(frameCount, Is.EqualTo(framesWhileBlocked));

        callbackDone.TrySetResult();
        await Expect(async () =>
        {
            await page.EvaluateAsync(@"() => {
      document.body.style.backgroundColor = document.body.style.backgroundColor === 'red' ? 'blue' : 'red';
    }");
            await EnsureSomeFrames(page);
            Assert.That(frameCount, Is.GreaterThan(framesWhileBlocked));
        }).ToPassAsync(new() { Timeout = 30000 });

        await page.Screencast.StopAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("SC-003 onFrame receives viewport size")]
    public async Task OnFrameReceivesViewportSize(IBrowser browser, Server server, string trace, string browserName, bool isMac, bool headless, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(trace == "on", "trace=on has different screencast image configuration");
        Info.Fixme(browserName == "firefox" && isMac && !headless, "wrong frame size in headed Firefox on Mac");

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 1000, Height = 400 } });
        var page = await context.NewPageAsync();

        var frames = new List<(float timestamp, int viewportWidth, int viewportHeight)>();
        await page.Screencast.StartAsync(new()
        {
            OnFrame = frame =>
            {
                frames.Add((frame.Timestamp, frame.ViewportWidth, frame.ViewportHeight));
                return Task.CompletedTask;
            },
            Size = new() { Width = 500, Height = 400 },
        });
        await page.GotoAsync(server.EmptyPage);
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();

        Assert.That(frames.Count, Is.GreaterThan(0));
        foreach (var frame in frames)
        {
            Assert.That(frame.viewportWidth, Is.EqualTo(1000));
            Assert.That(frame.viewportHeight, Is.EqualTo(400));
            Assert.That(frame.timestamp, Is.TypeOf<float>());
        }

        await context.CloseAsync();
    }

    [UpstreamTest("SC-004 start throws if screencast is already started")]
    public async Task StartThrowsIfScreencastIsAlreadyStarted(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 400 } });
        var page = await context.NewPageAsync();

        await page.Screencast.StartAsync(new() { OnFrame = _ => Task.CompletedTask });
        var error = await ThrowsAsync<PlaywrightException>(() => page.Screencast.StartAsync(new() { OnFrame = _ => Task.CompletedTask }));
        Assert.That(error.Message, Does.Contain("Screencast is already started"));

        await page.Screencast.StopAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("SC-005 start allows restart with different options after stop")]
    public async Task StartAllowsRestartWithDifferentOptionsAfterStop(IBrowser browser, string trace, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(trace == "on", "trace=on enables screencast with different options");

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 400 } });
        var page = await context.NewPageAsync();

        await page.Screencast.StartAsync(new() { OnFrame = _ => Task.CompletedTask, Size = new() { Width = 500, Height = 400 } });
        await page.Screencast.StopAsync();
        // Different options should succeed once the previous screencast is stopped.
        await page.Screencast.StartAsync(new() { OnFrame = _ => Task.CompletedTask, Size = new() { Width = 320, Height = 240 } });
        await page.Screencast.StopAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("SC-006 start returns a disposable that stops screencast")]
    public async Task StartReturnsADisposableThatStopsScreencast(IBrowser browser, Server server, string trace, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(trace == "on", "trace=on has different screencast image configuration");
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 400 } });
        var page = await context.NewPageAsync();

        var frames = new List<byte[]>();
        await page.Screencast.StartAsync(new()
        {
            OnFrame = frame =>
            {
                frames.Add(frame.Data);
                return Task.CompletedTask;
            },
            Size = new() { Width = 500, Height = 400 },
        });
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();

        var frameCountAfterDispose = frames.Count;
        Assert.That(frameCountAfterDispose, Is.GreaterThan(0));

        // No more frames should arrive after dispose.
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'blue'");
        await EnsureSomeFrames(page);
        Assert.That(frames.Count, Is.EqualTo(frameCountAfterDispose));

        await context.CloseAsync();
    }

    [UpstreamTest("SC-007 start/stop twice without path creates two files in artifactsDir")]
    public async Task StartStopTwiceWithoutPathCreatesTwoFilesInArtifactsDir(IBrowserType browserType, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        Info.Slow();
        var artifactsDir = testInfo.OutputPath("artifacts");
        var browser = await browserType.LaunchAsync(new(launchOptions) { ArtifactsDir = artifactsDir });
        var size = new ScreencastSize { Width = 800, Height = 800 };
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = size.Width, Height = size.Height } });
        var page = await context.NewPageAsync();

        await page.Screencast.StartAsync(new() { Path = testInfo.OutputPath("video1.webm"), Size = size });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();

        await page.Screencast.StartAsync(new() { Path = testInfo.OutputPath("video2.webm"), Size = size });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'blue'");
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();

        var videoFiles = Directory.GetFileSystemEntries(artifactsDir).Where(f => f.EndsWith(".webm", StringComparison.Ordinal)).ToArray();
        Assert.That(videoFiles, Has.Length.EqualTo(2));

        await context.CloseAsync();
        await browser.CloseAsync();
    }

    [UpstreamTest("SC-008 start should work when recordVideo is set")]
    public async Task StartShouldWorkWhenRecordVideoIsSet(IBrowser browser, TestInfo testInfo, BrowserNewContextOptions contextOptions)
    {
        Info.Slow();

        var autoDir = testInfo.OutputPath("auto");
        var manualDir = testInfo.OutputPath("manual");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            RecordVideoDir = autoDir,
        });
        var page = await context.NewPageAsync();

        await page.Screencast.StartAsync(new() { Path = Path.Combine(manualDir, "video.webm") });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'blue'");
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();
        var videoFiles1 = Directory.GetFileSystemEntries(manualDir).Where(f => f.EndsWith(".webm", StringComparison.Ordinal)).ToArray();
        Assert.That(videoFiles1, Has.Length.EqualTo(1));

        await context.CloseAsync();
        var videoFiles2 = Directory.GetFileSystemEntries(autoDir).Where(f => f.EndsWith(".webm", StringComparison.Ordinal)).ToArray();
        Assert.That(videoFiles2, Has.Length.EqualTo(1));
    }

    [UpstreamTest("SC-009 start should record video with the requested fps")]
    public async Task StartShouldRecordVideoWithTheRequestedFps(IBrowser browser, TestInfo testInfo, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var videoPath = testInfo.OutputPath("video.webm");
        await page.Screencast.StartAsync(new() { Path = videoPath, Fps = 60 });
        await EnsureSomeFrames(page);
        await page.Screencast.StopAsync();
        Assert.That(new VideoPlayer(videoPath).Fps, Is.EqualTo(60));
        await context.CloseAsync();
    }

    [UpstreamTest("SC-010 start should throw on invalid fps")]
    public async Task StartShouldThrowOnInvalidFps(IPage page, TestInfo testInfo)
    {
        var error = await Catch(() => page.Screencast.StartAsync(new() { Path = testInfo.OutputPath("video.webm"), Fps = -1 }));
        Assert.That(error.Message, Does.Contain("\"fps\" must be a positive number, got -1"));
    }

    [UpstreamTest("SC-011 start should fail when another recording is in progress")]
    public async Task StartShouldFailWhenAnotherRecordingIsInProgress(IPage page, string trace, TestInfo testInfo)
    {
        Info.Skip(trace == "on", "trace=on has different screencast image configuration");
        await page.Screencast.StartAsync(new() { Path = testInfo.OutputPath("video.webm") });
        var error = await Catch(() => page.Screencast.StartAsync(new() { Path = testInfo.OutputPath("video2.webm") }));
        Assert.That(error.Message, Does.Contain("Screencast is already started"));
    }

    [UpstreamTest("SC-012 stop should not fail when no recording is in progress")]
    public async Task StopShouldNotFailWhenNoRecordingIsInProgress(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        // stop() is a no-op when screencast is not started.
        await page.Screencast.StopAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("SC-013 start should finish when page is closed")]
    public async Task StartShouldFinishWhenPageIsClosed(IBrowser browser, TestInfo testInfo, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var videoPath = testInfo.OutputPath("video.webm");
        await page.Screencast.StartAsync(new() { Path = videoPath, Size = new() { Width = 800, Height = 800 } });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await EnsureSomeFrames(page);
        await page.CloseAsync();
        var error = await Catch(() => page.Screencast.StopAsync());
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
        await context.CloseAsync();
    }

    [UpstreamTest("SC-014 empty video")]
    public async Task EmptyVideo(IBrowser browser, TestInfo testInfo, BrowserNewContextOptions contextOptions)
    {
        var size = (width: 800, height: 800);
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = size.width, Height = size.height } });
        var page = await context.NewPageAsync();
        var videoPath = testInfo.OutputPath("empty-video.webm");
        await page.Screencast.StartAsync(new() { Path = videoPath, Size = new() { Width = size.width, Height = size.height } });
        await page.Screencast.StopAsync();
        await context.CloseAsync();
        ExpectFrames(videoPath, size, IsAlmostWhite);
    }

    [UpstreamTest("SC-015 start dispose stops recording")]
    public async Task StartDisposeStopsRecording(IBrowser browser, TestInfo testInfo, BrowserNewContextOptions contextOptions)
    {
        Info.Slow();
        var size = (width: 800, height: 800);
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = size.width, Height = size.height } });
        var page = await context.NewPageAsync();
        var videoPath = testInfo.OutputPath("dispose-video.webm");
        var disposable = await page.Screencast.StartAsync(new() { Path = videoPath, Size = new() { Width = size.width, Height = size.height } });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await EnsureSomeFrames(page);
        await disposable.DisposeAsync();
        ExpectRedFrames(videoPath, size);
        await context.CloseAsync();
    }

    private sealed record Pixel(int r, int g, int b, int alpha);

    private static bool IsAlmostWhite(Pixel pixel)
    {
        var (r, g, b, alpha) = pixel;
        return r > 185 && g > 185 && b > 185 && alpha == 255;
    }

    private static bool IsAlmostRed(Pixel pixel)
    {
        var (r, g, b, alpha) = pixel;
        return r > 185 && g < 70 && b < 70 && alpha == 255;
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

    private static (int width, int height) JpegDimensions(byte[] buffer)
    {
        var i = 2; // skip SOI marker (FF D8)
        while (i < buffer.Length - 8)
        {
            if (buffer[i] != 0xFF)
            {
                break;
            }
            var marker = buffer[i + 1];
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(i + 2));
            // SOF markers: C0 (baseline), C2 (progressive), C1, C3, C5-C7, C9-CB, CD-CF
            if ((marker >= 0xC0 && marker <= 0xC3) || (marker >= 0xC5 && marker <= 0xC7) ||
                (marker >= 0xC9 && marker <= 0xCB) || (marker >= 0xCD && marker <= 0xCF))
            {
                var height = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(i + 5));
                var width = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(i + 7));
                return (width, height);
            }
            i += 2 + segmentLength;
        }
        throw new Exception("Could not parse JPEG dimensions");
    }
}
