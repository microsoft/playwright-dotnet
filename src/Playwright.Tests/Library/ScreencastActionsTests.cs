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

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/screencast-actions.spec.ts.</summary>
public class ScreencastActionsTests : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Mode != "default", "Annotations use an open shadow root only in default mode");

    private static readonly ScreencastActionStyle style = new()
    {
        Point = "width: 20px; height: 20px; border-radius: 50%; background: red",
        Highlight = "outline: 2px solid #333",
    };

    [UpstreamTest("SC-016 should show annotation on click")]
    [LocalFixme("dotnet: ToHaveTextAsync(Regex) sends ignoreCase: false when the IgnoreCase option is not set, upstream sends no ignoreCase, so the driver removes the 'i' flag of the regex and the title 'Click Locator(\"button\")' does not match")]
    public async Task ShouldShowAnnotationOnClick()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000 });
        IgnoreErrors(page.ClickAsync("button"));

        await Expect(page.Locator("x-pw-title")).ToBeVisibleAsync();
        await Expect(page.Locator("x-pw-title")).ToHaveTextAsync(new Regex("click", RegexOptions.IgnoreCase));
        // Point and highlight are only shown when styled.
        await Expect(page.Locator("x-pw-screencast-highlight")).ToHaveCountAsync(0);
        await Expect(page.Locator("x-pw-screencast-point")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("SC-017 should render annotation styles")]
    public async Task ShouldRenderAnnotationStyles()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

#pragma warning disable CS0612
        await page.Screencast.ShowActionsAsync(new() { Duration = 5000, FontSize = 32, Style = MergeOptions(new ScreencastActionStyle { Title = "top: 100px; background-color: rgb(1, 2, 3)" }, style) });
#pragma warning restore CS0612
        var buttonBox = await page.Locator("button").BoundingBoxAsync();
        IgnoreErrors(page.ClickAsync("button"));

        // Highlight: styled box that covers the target.
        var highlight = page.Locator("x-pw-screencast-highlight");
        await Expect(highlight).ToBeVisibleAsync();
        Assert.That(await highlight.EvaluateAsync<string>("el => getComputedStyle(el).outlineWidth"), Is.EqualTo("2px"));
        var box = await highlight.BoundingBoxAsync();
        Assert.That(box.X, Is.EqualTo(buttonBox.X).Within(0.5 * Math.Pow(10, -0)));
        Assert.That(box.Y, Is.EqualTo(buttonBox.Y).Within(0.5 * Math.Pow(10, -0)));
        Assert.That(box.Width, Is.EqualTo(buttonBox.Width).Within(0.5 * Math.Pow(10, -0)));
        Assert.That(box.Height, Is.EqualTo(buttonBox.Height).Within(0.5 * Math.Pow(10, -0)));

        // Point: styled marker centered on the click point, Firefox rounds the click point.
        var pointBox = await page.Locator("x-pw-screencast-point").BoundingBoxAsync();
        Assert.That(pointBox.Width, Is.EqualTo(20d));
        Assert.That(Math.Abs(pointBox.X + 10 - (buttonBox.X + (buttonBox.Width / 2))), Is.LessThanOrEqualTo(1));
        Assert.That(Math.Abs(pointBox.Y + 10 - (buttonBox.Y + (buttonBox.Height / 2))), Is.LessThanOrEqualTo(1));

        // Title: style applies on top of deprecated fontSize, position wins over style.
        var title = page.Locator("x-pw-title");
        await Expect(title).ToBeVisibleAsync();
        var titleStyle = await title.EvaluateAsync<JsonElement>(@"el => {
    const cs = getComputedStyle(el);
    return {
      color: cs.color, borderRadius: cs.borderRadius, padding: cs.padding, backgroundColor: cs.backgroundColor,
      top: el.style.top, right: el.style.right, fontSize: el.style.fontSize,
    };
  }");
        Assert.That(titleStyle.GetProperty("backgroundColor").GetString(), Is.EqualTo("rgb(1, 2, 3)"));
        Assert.That(titleStyle.GetProperty("color").GetString(), Is.EqualTo("rgb(255, 255, 255)"));
        Assert.That(titleStyle.GetProperty("borderRadius").GetString(), Is.EqualTo("6px"));
        Assert.That(titleStyle.GetProperty("padding").GetString(), Is.EqualTo("6px"));
        Assert.That(titleStyle.GetProperty("top").GetString(), Is.EqualTo("6px"));
        Assert.That(titleStyle.GetProperty("right").GetString(), Is.EqualTo("6px"));
        Assert.That(titleStyle.GetProperty("fontSize").GetString(), Is.EqualTo("32px"));

        await context.CloseAsync();
    }

    private static readonly object[] Positions =
    [
        new object[] { AnnotatePosition.TopLeft, new Dictionary<string, string> { ["top"] = "6px", ["left"] = "6px" } },
        new object[] { AnnotatePosition.Top, new Dictionary<string, string> { ["top"] = "6px", ["left"] = "50%" } },
        new object[] { AnnotatePosition.BottomLeft, new Dictionary<string, string> { ["bottom"] = "6px", ["left"] = "6px" } },
        new object[] { AnnotatePosition.Bottom, new Dictionary<string, string> { ["bottom"] = "6px", ["left"] = "50%" } },
        new object[] { AnnotatePosition.BottomRight, new Dictionary<string, string> { ["bottom"] = "6px", ["right"] = "6px" } },
    ];

    [UpstreamTest("SC-018 should position title at ${position}")]
    [TestCaseSource(nameof(Positions))]
    public async Task ShouldPositionTitleAt(AnnotatePosition position, Dictionary<string, string> expected)
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000, Position = position });
        IgnoreErrors(page.ClickAsync("button"));

        var title = page.Locator("x-pw-title");
        await Expect(title).ToBeVisibleAsync();

        var titleStyle = await title.EvaluateAsync<JsonElement>(@"el => ({
      top: el.style.top,
      bottom: el.style.bottom,
      left: el.style.left,
      right: el.style.right,
    })");

        foreach (var (key, value) in expected)
        {
            Assert.That(titleStyle.GetProperty(key).GetString(), Is.EqualTo(value));
        }

        await context.CloseAsync();
    }

    [UpstreamTest("SC-019 should clear annotation after duration")]
    public async Task ShouldClearAnnotationAfterDuration()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 1000, Style = style });
        await page.ClickAsync("button");

        await Expect(page.Locator("x-pw-screencast-point")).ToHaveCountAsync(0);
        await Expect(page.Locator("x-pw-screencast-highlight")).ToHaveCountAsync(0);
        await Expect(page.Locator("x-pw-title")).ToBeHiddenAsync();

        await context.CloseAsync();
    }

    [UpstreamTest("SC-020 should annotate fill action")]
    [LocalFixme("dotnet: ToHaveTextAsync(Regex) sends ignoreCase: false when the IgnoreCase option is not set, upstream sends no ignoreCase, so the driver removes the 'i' flag of the regex and the title 'Fill \"hello\" Locator(\"textarea\")' does not match")]
    public async Task ShouldAnnotateFillAction()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/textarea.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000 });
        IgnoreErrors(page.FillAsync("textarea", "hello"));

        var title = page.Locator("x-pw-title");
        await Expect(title).ToBeVisibleAsync();
        await Expect(title).ToHaveTextAsync(new Regex("fill", RegexOptions.IgnoreCase));

        await context.CloseAsync();
    }

    [UpstreamTest("SC-021 should stop showing actions after dispose")]
    public async Task ShouldStopShowingActionsAfterDispose()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        var actions = await page.Screencast.ShowActionsAsync(new() { Duration = 1000 });
        await page.ClickAsync("button");
        await actions.DisposeAsync();

        await page.GotoAsync(Server.Prefix + "/input/button.html");
        await page.ClickAsync("button");

        await Expect(page.Locator("x-pw-title")).ToBeHiddenAsync();

        await context.CloseAsync();
    }

    [UpstreamTest("SC-022 should stop showing actions after hideActions")]
    public async Task ShouldStopShowingActionsAfterHideActions()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 1000 });
        await page.ClickAsync("button");
        await page.Screencast.HideActionsAsync();

        await page.GotoAsync(Server.Prefix + "/input/button.html");
        await page.ClickAsync("button");

        await Expect(page.Locator("x-pw-title")).ToBeHiddenAsync();

        await context.CloseAsync();
    }

    [UpstreamTest("SC-023 should render an action cursor that animates to the click point")]
    public async Task ShouldRenderAnActionCursorThatAnimatesToTheClickPoint()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000 });
        IgnoreErrors(page.ClickAsync("button"));

        var cursor = page.Locator("x-pw-action-cursor");
        await Expect(cursor).ToBeVisibleAsync();

        var initial = await cursor.EvaluateAsync<JsonElement>(@"el => ({
    top: el.style.top,
    left: el.style.left,
  })");
        Assert.That(initial.GetProperty("top").GetString(), Does.Match(@"\d+px"));
        Assert.That(initial.GetProperty("left").GetString(), Does.Match(@"\d+px"));

        await context.CloseAsync();
    }

    [UpstreamTest("SC-024 cursor moves between two pointer actions")]
    public async Task CursorMovesBetweenTwoPointerActions()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync(@"
    <div style=""position: fixed; top: 20px; left: 20px; width: 60px; height: 60px;"" id=""a"">A</div>
    <div style=""position: fixed; bottom: 20px; right: 20px; width: 60px; height: 60px;"" id=""b"">B</div>
  ");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000 });
        var cursor = page.Locator("x-pw-action-cursor");

        IgnoreErrors(page.ClickAsync("#a", new() { Force = true }));
        await Expect(cursor).ToBeVisibleAsync();
        var first = await cursor.EvaluateAsync<object>("el => ({ top: el.style.top, left: el.style.left })");

        IgnoreErrors(page.ClickAsync("#b", new() { Force = true }));
        await Expect(async () =>
        {
            ExpectNotToEqual(await cursor.EvaluateAsync<object>("el => ({ top: el.style.top, left: el.style.left })"), first);
        }).ToPassAsync();

        await context.CloseAsync();
    }

    [UpstreamTest("SC-025 cursor: \"none\" suppresses the action cursor decoration")]
    public async Task CursorNoneSuppressesTheActionCursorDecoration()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000, Cursor = ScreencastCursor.None });
        IgnoreErrors(page.ClickAsync("button"));

        // The title still renders, but the cursor does not.
        await Expect(page.Locator("x-pw-title")).ToBeVisibleAsync();
        await Expect(page.Locator("x-pw-action-cursor")).ToBeHiddenAsync();

        await context.CloseAsync();
    }

    [UpstreamTest("SC-026 cursor stays at the last action point until hideActions")]
    public async Task CursorStaysAtTheLastActionPointUntilHideActions()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 100 });
        await page.ClickAsync("button");
        await Expect(page.Locator("x-pw-title")).ToBeHiddenAsync();
        var cursor = page.Locator("x-pw-action-cursor");
        await Expect(cursor).ToBeVisibleAsync();
        var position = await cursor.EvaluateAsync<object>("el => ({ top: el.style.top, left: el.style.left })");

        await page.GotoAsync(Server.EmptyPage);
        await Expect(cursor).ToBeVisibleAsync();
        ExpectToEqual(await cursor.EvaluateAsync<object>("el => ({ top: el.style.top, left: el.style.left })"), position);

        await page.Screencast.HideActionsAsync();
        await Expect(cursor).ToBeHiddenAsync();

        await context.CloseAsync();
    }

    [UpstreamTest("SC-027 should survive navigation")]
    public async Task ShouldSurviveNavigation()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.Screencast.ShowActionsAsync(new() { Duration = 5000 });
        IgnoreErrors(page.ClickAsync("button"));

        await Expect(page.Locator("x-pw-title")).ToBeVisibleAsync();

        await page.GotoAsync(Server.Prefix + "/input/button.html");
        IgnoreErrors(page.ClickAsync("button"));

        await Expect(page.Locator("x-pw-title")).ToBeVisibleAsync();

        await context.CloseAsync();
    }
}
