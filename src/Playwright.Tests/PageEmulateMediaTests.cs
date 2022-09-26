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

public class PageEmulateMediaTests : PageTestEx
{
    [PlaywrightTest("page-emulate-media.spec.ts", "should emulate scheme work")]
    public async Task ShouldEmulateSchemeWork()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
    }

    [PlaywrightTest("page-emulate-media.spec.ts", "should default to light")]
    public async Task ShouldDefaultToLight()
    {
        Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Null });
        Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
    }

    [PlaywrightTest("page-emulate-media.spec.ts", "should work during navigation")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkDuringNavigation()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        var navigated = Page.GotoAsync(Server.EmptyPage);

        for (int i = 0; i < 9; i++)
        {
            await Page.EmulateMediaAsync(new() { ColorScheme = i % 2 == 0 ? ColorScheme.Dark : ColorScheme.Light });
            await Task.Delay(1);
        }
        await navigated;

        Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
    }

    [PlaywrightTest("page-emulate-media.spec.ts", "should work in popup")]
    public async Task ShouldWorkInPopup()
    {
        await using (var context = await Browser.NewContextAsync(new()
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

        await using (var context = await Browser.NewContextAsync(new()
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
    public async Task ShouldWorkInCrossProcessIframe()
    {
        await using var context = await Browser.NewContextAsync(new()
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
    public async Task ShouldEmulateType()
    {
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
        await Page.EmulateMediaAsync(new() { Media = Media.Print });
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
        await Page.EmulateMediaAsync();
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
        await Page.EmulateMediaAsync(new() { Media = Media.Null });
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
    }

    [PlaywrightTest("page-emulate-media.spec.ts", "should emulate reduced motion")]
    public async Task ShouldEmulateReducedMotion()
    {
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('(prefers-reduced-motion: no-preference)').matches"));
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('(prefers-reduced-motion: reduce)').matches"));
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('(prefers-reduced-motion: no-preference)').matches"));
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        Assert.False(await Page.EvaluateAsync<bool>("matchMedia('(prefers-reduced-motion: reduce)').matches"));
        Assert.True(await Page.EvaluateAsync<bool>("matchMedia('(prefers-reduced-motion: no-preference)').matches"));
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Null });
    }

    [PlaywrightTest("page-emulate-media.spec.ts", "should emulate forcedColors")]
    [Skip(SkipAttribute.Targets.Webkit)] // see: https://bugs.webkit.org/show_bug.cgi?id=225281
    public async Task ShouldEmulateForcedColors()
    {
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"));
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.None });
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"));
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"));
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"));
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"));
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Null });
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"));
    }
}
