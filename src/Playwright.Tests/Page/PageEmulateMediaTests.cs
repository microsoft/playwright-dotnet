/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-emulate-media.spec.ts.</summary>
public class PageEmulateMediaTests
{
    // .NET: the toMatchMedia matcher of baseExpect.extend() is a helper; `not` is expect(page).not.toMatchMedia().
    private static async Task ExpectToMatchMedia(IPage page, string mediaQuery, bool not = false)
    {
        bool pass;
        try
        {
            pass = await page.EvaluateAsync<bool>("mediaQuery => matchMedia(mediaQuery).matches", mediaQuery);
        }
        catch (Exception)
        {
            pass = false;
        }
        if (pass)
        {
            Assert.That(pass, Is.EqualTo(!not), $"Expected \"{mediaQuery}\" not to match, but it did");
        }
        else
        {
            Assert.That(pass, Is.EqualTo(!not), $"Expected \"{mediaQuery}\" to match, but it did not");
        }
    }

    [UpstreamTest("EM-001 should emulate type @smoke")]
    public async Task ShouldEmulateType(IPage page)
    {
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('screen').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('print').matches"), Is.EqualTo(false));
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('screen').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('print').matches"), Is.EqualTo(true));
        await page.EmulateMediaAsync(new());
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('screen').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('print').matches"), Is.EqualTo(true));
        await page.EmulateMediaAsync(new() { Media = Media.Null });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('screen').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('print').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("EM-003 should emulate colorScheme should work @smoke")]
    public async Task ShouldEmulateColorSchemeShouldWork(IPage page)
    {
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(false));
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("EM-004 should default to light")]
    public async Task ShouldDefaultToLight(IPage page)
    {
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(false));

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(false));

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Null });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-006 should work during navigation")]
    public async Task ShouldWorkDuringNavigation(IPage page, Server server)
    {
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        var navigated = page.GotoAsync(server.EmptyPage);
        for (var i = 0; i < 9; i++)
        {
            await PromiseAll(
                page.EmulateMediaAsync(new() { ColorScheme = new[] { ColorScheme.Dark, ColorScheme.Light }[i & 1] }),
                Task.Delay(1));
        }
        await navigated;
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-007 should change the actual colors in css")]
    public async Task ShouldChangeTheActualColorsInCss(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      @media (prefers-color-scheme: dark) {
        div {
          background: black;
          color: white;
        }
      }
      @media (prefers-color-scheme: light) {
        div {
          background: white;
          color: black;
        }
      }

    </style>
    <div>Hello</div>
  ");
        Task<string> getBackgroundColor()
        {
            return page.EvalOnSelectorAsync<string>("div", "div => window.getComputedStyle(div).backgroundColor");
        }

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        Assert.That(await getBackgroundColor(), Is.EqualTo("rgb(255, 255, 255)"));

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        Assert.That(await getBackgroundColor(), Is.EqualTo("rgb(0, 0, 0)"));

        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        Assert.That(await getBackgroundColor(), Is.EqualTo("rgb(255, 255, 255)"));
    }

    [UpstreamTest("EM-008 should emulate reduced motion")]
    public async Task ShouldEmulateReducedMotion(IPage page)
    {
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"), Is.EqualTo(true));
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"), Is.EqualTo(false));
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"), Is.EqualTo(true));
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Null });
    }

    [UpstreamTest("EM-009 should keep reduced motion and color emulation after reload")]
    public async Task ShouldKeepReducedMotionAndColorEmulationAfterReload(IPage page, Server server)
    {
        // Pre-conditions
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(false));

        // Emulation
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(true));

        // Force CanonicalBrowsingContext replacement in Firefox.
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        await page.GotoAsync(server.EmptyPage);

        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-010 should emulate forcedColors ")]
    public async Task ShouldEmulateForcedColors(IPage page, string browserName)
    {
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"), Is.EqualTo(true));
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.None });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(false));
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(true));
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Null });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-011 should emulate contrast ")]
    public async Task ShouldEmulateContrast(IPage page)
    {
        await ExpectToMatchMedia(page, "(prefers-contrast: no-preference)");
        await page.EmulateMediaAsync(new() { Contrast = Contrast.NoPreference });
        await ExpectToMatchMedia(page, "(prefers-contrast: no-preference)");
        await ExpectToMatchMedia(page, "(prefers-contrast: more)", not: true);
        await page.EmulateMediaAsync(new() { Contrast = Contrast.More });
        await ExpectToMatchMedia(page, "(prefers-contrast: no-preference)", not: true);
        await ExpectToMatchMedia(page, "(prefers-contrast: more)");
        await page.EmulateMediaAsync(new() { Contrast = Contrast.Null });
        await ExpectToMatchMedia(page, "(prefers-contrast: no-preference)");
    }

    [UpstreamTest("EM-012 should report hover and fine pointer for desktop")]
    public async Task ShouldReportHoverAndFinePointerForDesktop(IPage page, bool isAndroid)
    {
        It.Skip(isAndroid);

        await ExpectToMatchMedia(page, "(hover: hover)");
        await ExpectToMatchMedia(page, "(pointer: fine)");
    }
}
