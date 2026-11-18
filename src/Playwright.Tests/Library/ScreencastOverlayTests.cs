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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/screencast-overlay.spec.ts.</summary>
public class ScreencastOverlayTests : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Mode != "default", "Overlay uses an open shadow root only in default mode");

    private static int[] Pixel(byte[] screenshot, int x, int y)
    {
        // .NET: PNG.sync.read(screenshot)
        var png = ImageComparator.Decode("image/png", screenshot);
        var offset = ((y * png.Width) + x) * 4;
        return [png.Data[offset], png.Data[offset + 1], png.Data[offset + 2]];
    }

    [UpstreamTest("SC-028 should add and remove overlay")]
    public async Task ShouldAddAndRemoveOverlay()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var disposable = await page.Screencast.ShowOverlayAsync("<div id=\"my-overlay\">Hello Overlay</div>");
        await Expect(page.Locator("x-pw-user-overlays")).ToBeVisibleAsync();
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(1);
        await Expect(page.Locator("#my-overlay")).ToHaveTextAsync("Hello Overlay");

        await disposable.DisposeAsync();
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("SC-029 should add multiple overlays")]
    public async Task ShouldAddMultipleOverlays()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var d1 = await page.Screencast.ShowOverlayAsync("<div id=\"overlay-1\">First</div>");
        var d2 = await page.Screencast.ShowOverlayAsync("<div id=\"overlay-2\">Second</div>");
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(2);
        await Expect(page.Locator("#overlay-1")).ToHaveTextAsync("First");
        await Expect(page.Locator("#overlay-2")).ToHaveTextAsync("Second");

        await d1.DisposeAsync();
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(1);
        await Expect(page.Locator("#overlay-2")).ToHaveTextAsync("Second");

        await d2.DisposeAsync();
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("SC-030 should hide and show overlays")]
    public async Task ShouldHideAndShowOverlays()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.Screencast.ShowOverlayAsync("<div id=\"my-overlay\">Visible</div>");
        await Expect(page.Locator("x-pw-user-overlays")).ToBeVisibleAsync();

        await page.Screencast.HideOverlaysAsync();
        await Expect(page.Locator("x-pw-user-overlays")).ToBeHiddenAsync();

        await page.Screencast.ShowOverlaysAsync();
        await Expect(page.Locator("x-pw-user-overlays")).ToBeVisibleAsync();
        await Expect(page.Locator("#my-overlay")).ToHaveTextAsync("Visible");

        await context.CloseAsync();
    }

    [UpstreamTest("SC-031 should survive navigation")]
    public async Task ShouldSurviveNavigation()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.Screencast.ShowOverlayAsync("<div id=\"persistent\">Survives Reload</div>");
        await Expect(page.Locator("#persistent")).ToHaveTextAsync("Survives Reload");

        await page.GotoAsync(Server.EmptyPage);
        await Expect(page.Locator("#persistent")).ToHaveTextAsync("Survives Reload");

        await page.ReloadAsync();
        await Expect(page.Locator("#persistent")).ToHaveTextAsync("Survives Reload");

        await context.CloseAsync();
    }

    [UpstreamTest("SC-032 should remove overlay and not restore after navigation")]
    public async Task ShouldRemoveOverlayAndNotRestoreAfterNavigation()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var disposable = await page.Screencast.ShowOverlayAsync("<div id=\"temp\">Temporary</div>");
        await Expect(page.Locator("#temp")).ToHaveTextAsync("Temporary");

        await disposable.DisposeAsync();
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(0);

        await page.ReloadAsync();
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("SC-033 should sanitize scripts from overlay html")]
    public async Task ShouldSanitizeScriptsFromOverlayHtml()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.Screencast.ShowOverlayAsync("<div id=\"safe\">Safe</div><script>window.__injected = true</script>");
        await Expect(page.Locator("#safe")).ToHaveTextAsync("Safe");
        Assert.That(await page.EvaluateAsync<object>("() => window.__injected"), Is.Null);

        await context.CloseAsync();
    }

    [UpstreamTest("SC-034 should strip event handlers from overlay html")]
    public async Task ShouldStripEventHandlersFromOverlayHtml()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.Screencast.ShowOverlayAsync("<div id=\"clean\" onclick=\"window.__clicked=true\">Click me</div>");
        await Expect(page.Locator("#clean")).ToHaveTextAsync("Click me");
        var hasOnclick = await page.Locator("#clean").EvaluateAsync<bool>("el => el.hasAttribute('onclick')");
        Assert.That(hasOnclick, Is.EqualTo(false));

        await context.CloseAsync();
    }

    [UpstreamTest("SC-035 should auto-remove overlay after timeout")]
    public async Task ShouldAutoRemoveOverlayAfterTimeout()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.Screencast.ShowOverlayAsync("<div id=\"timed\">Temporary</div>", new() { Duration = 1 });
        await Expect(page.Locator(".x-pw-user-overlay")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("SC-036 should allow styles in overlay html")]
    public async Task ShouldAllowStylesInOverlayHtml()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.Screencast.ShowOverlayAsync("<div id=\"styled\" style=\"color: red; font-size: 20px;\">Styled</div>");
        await Expect(page.Locator("#styled")).ToHaveTextAsync("Styled");
        var color = await page.Locator("#styled").EvaluateAsync<string>("el => getComputedStyle(el).color");
        Assert.That(color, Is.EqualTo("rgb(255, 0, 0)"));

        await context.CloseAsync();
    }

    private static readonly Dictionary<string, string> topLayerPages = new()
    {
        ["dialog"] = @"
    <dialog id=""target"" style=""position: fixed; inset: 0; margin: 0; width: 100vw; height: 100vh; padding: 0; border: 0; background: rgb(200, 200, 200);""></dialog>
    <button onclick=""target.showModal()"">Open</button>
  ",
        ["popover"] = @"
    <div id=""target"" popover=""manual"" style=""inset: 0; margin: 0; width: 100%; height: 100%; max-width: none; max-height: none; padding: 0; border: 0; background: rgb(200, 200, 200);""></div>
    <button onclick=""target.showPopover()"">Open</button>
  ",
    };

    private const string redOverlay = "<div style=\"position: absolute; top: 50px; left: 50px; width: 200px; height: 100px; background: rgb(255, 0, 0);\"></div>";

    // .NET: Object.entries(topLayerPages)
    private static IEnumerable<object[]> TopLayerPages() => topLayerPages.Select(entry => new object[] { entry.Key, entry.Value });

    [UpstreamTest("SC-037 should show overlay above ${kind} opened before it")]
    [TestCaseSource(nameof(TopLayerPages))]
    public async Task ShouldShowOverlayAboveOpenedBeforeIt(string kind, string content)
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync(content);
        var disposable = await page.Screencast.ShowOverlayAsync("<div></div>");
        await disposable.DisposeAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Open" }).ClickAsync();
        Assert.That(Pixel(await page.ScreenshotAsync(), 100, 100), Is.EqualTo(new[] { 200, 200, 200 }));

        await page.Screencast.ShowOverlayAsync(redOverlay);
        Assert.That(Pixel(await page.ScreenshotAsync(), 100, 100), Is.EqualTo(new[] { 255, 0, 0 }));

        await context.CloseAsync();
    }

    [UpstreamTest("SC-038 should keep overlay above ${kind} opened after it")]
    [TestCaseSource(nameof(TopLayerPages))]
    public async Task ShouldKeepOverlayAboveOpenedAfterIt(string kind, string content)
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync(content);
        await page.Screencast.ShowOverlayAsync(redOverlay);
        Assert.That(Pixel(await page.ScreenshotAsync(), 100, 100), Is.EqualTo(new[] { 255, 0, 0 }));

        await page.GetByRole(AriaRole.Button, new() { Name = "Open" }).ClickAsync();
        await Expect(async () => Assert.That(Pixel(await page.ScreenshotAsync(), 100, 100), Is.EqualTo(new[] { 255, 0, 0 }))).ToPassAsync();

        await context.CloseAsync();
    }
}
