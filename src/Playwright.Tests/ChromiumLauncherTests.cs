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

namespace Microsoft.Playwright.Tests.Firefox;

public class ChromiumLauncherTests : PlaywrightTestEx
{
    [PlaywrightTest("chromium/launcher.spec.ts", "should return background pages")]
    [Skip(SkipAttribute.Targets.Webkit, SkipAttribute.Targets.Firefox)]
    public async Task ShouldReturnBackgroundPages()
    {
        using var userDataDir = new TempDirectory();
        var extensionPath = TestUtils.GetAsset("simple-extension");
        var extensionOptions = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = false,
            Args = new[] {
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}",
            },
        };
        var context = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, extensionOptions);
        var backgroundPages = context.BackgroundPages;
        var backgroundPage = backgroundPages.Count > 0
            ? backgroundPages[0]
            : await WaitForBackgroundPage(context);
        Assert.NotNull(backgroundPage);
        Assert.Contains(backgroundPage, context.BackgroundPages.ToList());
        Assert.False(context.Pages.Contains(backgroundPage));
        await context.CloseAsync();
        Assert.IsEmpty(context.Pages);
        Assert.IsEmpty(context.BackgroundPages);
    }

    [PlaywrightTest("chromium/launcher.spec.ts", "should return background pages when recording video")]
    [Skip(SkipAttribute.Targets.Webkit, SkipAttribute.Targets.Firefox)]
    public async Task ShouldReturnBackgroundPagesWhenRecordingVideo()
    {
        using var tempDirectory = new TempDirectory();
        using var userDataDir = new TempDirectory();
        var extensionPath = TestUtils.GetAsset("simple-extension");
        var extensionOptions = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = false,
            Args = new[] {
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}",
            },
            RecordVideoDir = tempDirectory.Path,
        };
        var context = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, extensionOptions);
        var backgroundPages = context.BackgroundPages;

        var backgroundPage = backgroundPages.Count > 0
            ? backgroundPages[0]
            : await WaitForBackgroundPage(context);
        Assert.NotNull(backgroundPage);
        Assert.Contains(backgroundPage, context.BackgroundPages.ToList());
        Assert.False(context.Pages.Contains(backgroundPage));
        await context.CloseAsync();
    }

    private async Task<IPage> WaitForBackgroundPage(IBrowserContext context)
    {
        var tsc = new TaskCompletionSource<IPage>();
        context.BackgroundPage += (_, e) => tsc.TrySetResult(e);
        return await tsc.Task;
    }
}
