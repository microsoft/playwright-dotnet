/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

///<playwright-file>screencast.spec.ts</playwright-file>
public class ScreencastTests : BrowserTestEx
{
    [PlaywrightTest("screencast.spec.ts", "videoSize should require videosPath")]
    public async Task VideoSizeShouldRequireVideosPath()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new()
        {
            RecordVideoSize = new() { Height = 100, Width = 100 }
        }));

        StringAssert.Contains("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified", exception.Message);
    }

    [PlaywrightTest()]
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

    [PlaywrightTest("screencast.spec.ts", "should work with relative path for recordVideo.dir")]
    public async Task ShouldWorkWithRelativePathForRecordVideoDir()
    {
        using var tempDirectory = new TempDirectory();
        var context = await Browser.NewContextAsync(new()
        {
            RecordVideoDir = Path.Combine(Environment.CurrentDirectory, tempDirectory.Path),
            RecordVideoSize = new() { Height = 240, Width = 320 }
        });

        var page = await context.NewPageAsync();
        var videoPath = await page.Video.PathAsync();
        await context.CloseAsync();
        Assert.True(new FileInfo(videoPath).Exists);
    }

    [PlaywrightTest("screencast.spec.ts", "should capture static page")]
    [Skip(SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Windows)]
    public async Task ShouldCaptureStaticPage()
    {
        using var tempDirectory = new TempDirectory();
        var context = await Browser.NewContextAsync(new()
        {
            RecordVideoDir = tempDirectory.Path,
            RecordVideoSize = new() { Height = 100, Width = 100 }
        });

        var page = await context.NewPageAsync();
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await Task.Delay(1000);
        await context.CloseAsync();

        Assert.IsNotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
    }

    [PlaywrightTest("screencast.spec.ts", "should expose video path")]
    public async Task ShouldExposeVideoPath()
    {
        using var tempDirectory = new TempDirectory();
        var context = await Browser.NewContextAsync(new()
        {
            RecordVideoDir = tempDirectory.Path,
            RecordVideoSize = new() { Height = 100, Width = 100 }
        });

        var page = await context.NewPageAsync();
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        string path = await page.Video.PathAsync();
        StringAssert.Contains(tempDirectory.Path, path);
        await context.CloseAsync();

        Assert.True(new FileInfo(path).Exists);
    }

    [PlaywrightTest("screencast.spec.ts", "should expose video path blank page")]
    public async Task ShouldExposeVideoPathBlankPage()
    {
        using var tempDirectory = new TempDirectory();
        var context = await Browser.NewContextAsync(new()
        {
            RecordVideoDir = tempDirectory.Path,
            RecordVideoSize = new() { Height = 100, Width = 100 }
        });

        var page = await context.NewPageAsync();
        string path = await page.Video.PathAsync();
        StringAssert.Contains(tempDirectory.Path, path);
        await context.CloseAsync();

        Assert.True(new FileInfo(path).Exists);
    }

    [PlaywrightTest("screencast.spec.ts", "should capture static page in persistent context")]
    [Skip(SkipAttribute.Targets.Webkit, SkipAttribute.Targets.Firefox)]
    public async Task ShouldCaptureStaticPageInPersistentContext()
    {
        using var userDirectory = new TempDirectory();
        using var tempDirectory = new TempDirectory();
        var context = await BrowserType.LaunchPersistentContextAsync(userDirectory.Path, new()
        {
            RecordVideoDir = tempDirectory.Path,
            RecordVideoSize = new() { Height = 100, Width = 100 },
        });

        var page = await context.NewPageAsync();
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await Task.Delay(1000);
        await context.CloseAsync();

        Assert.IsNotEmpty(new DirectoryInfo(tempDirectory.Path).GetFiles("*.webm"));
    }


    [PlaywrightTest("screencast.spec.ts", "video.path()/saveAs() does not hang immediately after launchPersistentContext and context.close()")]
    public async Task VideoPathSaveAsDoesNotHangImmediatelyAfterLaunchPersistentContextAndContextClose()
    {
        using var userDirectory = new TempDirectory();
        using var tempDirectory = new TempDirectory();

        var context = await BrowserType.LaunchPersistentContextAsync(userDirectory.Path, new()
        {
            RecordVideoDir = tempDirectory.Path
        });
        var page = context.Pages[0];
        await context.CloseAsync();
        try
        {
            await page.Video.PathAsync();
        }
        catch (Exception)
        {
        }
        try
        {
            await page.Video.SaveAsAsync("video.mp4");
        }
        catch (Exception)
        {
        }
        try
        {
            await page.Video.DeleteAsync();
        }
        catch (Exception)
        {
        }
    }

    [PlaywrightTest("screencast.spec.ts", "saveAs should throw when no video frames")]
    public async Task SaveAsShouldThrowWhenNoVideoFrames()
    {
        using var tempDirectory = new TempDirectory();
        var context = await Browser.NewContextAsync(new()
        {
            RecordVideoDir = tempDirectory.Path,
            RecordVideoSize = new() { Width = 320, Height = 240 },
            ViewportSize = new() { Width = 320, Height = 240 }
        });

        var page = await context.NewPageAsync();
        var (popup, _) = await TaskUtils.WhenAll(
            page.Context.WaitForPageAsync(),
            page.EvaluateAsync("() => window.open('about:blank').close()")
        );
        await page.CloseAsync();

        var saveAsPath = Path.Combine(tempDirectory.Path, "my-video.webm");
        Exception exception = null;
        try
        {
            await popup.Video.SaveAsAsync(saveAsPath);
        }
        catch (Exception e)
        {
            exception = e;
        }
        // WebKit pauses renderer before win.close() and actually writes something,
        // and other browsers are sometimes fast as well.
        if (!File.Exists(saveAsPath))
            StringAssert.Contains("Page did not produce any video frames", exception.Message);
        await context.CloseAsync();
    }

    [PlaywrightTest("screencast.spec.ts", "start throws if screencast is already started")]
    public async Task StartThrowsIfScreencastIsAlreadyStarted()
    {
        var context = await Browser.NewContextAsync(new() { ViewportSize = new() { Width = 500, Height = 400 } });
        var page = await context.NewPageAsync();

        await page.Screencast.StartAsync(new() { OnFrame = _ => Task.CompletedTask });
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            page.Screencast.StartAsync(new() { OnFrame = _ => Task.CompletedTask }));
        StringAssert.Contains("Screencast is already started", exception.Message);

        await page.Screencast.StopAsync();
        await context.CloseAsync();
    }

    [PlaywrightTest("screencast.spec.ts", "start should record video to file")]
    public async Task StartShouldRecordVideoToFile()
    {
        using var tempDirectory = new TempDirectory();
        var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 500, Height = 400 }
        });
        var page = await context.NewPageAsync();

        var videoPath = Path.Combine(tempDirectory.Path, "video.webm");
        await page.Screencast.StartAsync(new() { Path = videoPath });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await Task.Delay(1000);
        await page.Screencast.StopAsync();

        Assert.True(File.Exists(videoPath));
        Assert.Greater(new FileInfo(videoPath).Length, 0);
        await context.CloseAsync();
    }

    [PlaywrightTest("screencast.spec.ts", "start delivers frames via onFrame callback")]
    public async Task StartDeliversFramesViaOnFrameCallback()
    {
        var context = await Browser.NewContextAsync(new() { ViewportSize = new() { Width = 500, Height = 400 } });
        var page = await context.NewPageAsync();

        var frames = new List<byte[]>();
        await page.Screencast.StartAsync(new()
        {
            OnFrame = async frame =>
            {
                frames.Add(frame.Data);
            }
        });
        await page.EvaluateAsync("() => document.body.style.backgroundColor = 'red'");
        await Task.Delay(1000);
        await page.Screencast.StopAsync();

        Assert.Greater(frames.Count, 0);
        // Each frame must be a valid JPEG (starts with FF D8).
        foreach (var frame in frames)
        {
            Assert.AreEqual(0xFF, frame[0]);
            Assert.AreEqual(0xD8, frame[1]);
        }
        await context.CloseAsync();
    }
}
