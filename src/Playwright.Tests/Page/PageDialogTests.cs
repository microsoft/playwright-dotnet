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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-dialog.spec.ts.</summary>
public class PageDialogTests
{
    [UpstreamTest("DI-001 should fire")]
    public async Task ShouldFire(IPage page, Server server)
    {
        page.Dialog += (_, dialog) =>
        {
            Assert.That(dialog.Type, Is.EqualTo("alert"));
            Assert.That(dialog.DefaultValue, Is.EqualTo(""));
            Assert.That(dialog.Message, Is.EqualTo("yo"));
            IgnoreErrors(dialog.AcceptAsync());
        };
        await page.EvaluateAsync("() => alert('yo')");
    }

    [UpstreamTest("DI-002 should fire dialogclosed when dialog is accepted")]
    public async Task ShouldFireDialogclosedWhenDialogIsAccepted(IPage page)
    {
        var closed = new List<IDialog>();
        page.DialogClosed += (_, dialog) => closed.Add(dialog);
        IDialog opened = null;
        page.Dialog += (_, dialog) =>
        {
            opened = dialog;
            IgnoreErrors(dialog.AcceptAsync());
        };
        await page.EvaluateAsync("() => alert('yo')");
        await Expect(() => Assert.That(closed.Count, Is.EqualTo(1))).ToPassAsync();
        Assert.That(closed[0], Is.SameAs(opened));
        // Perform some roundtrips to ensure the event does not fire twice.
        await page.EvaluateAsync("() => 1");
        await page.EvaluateAsync("() => 1");
        Assert.That(closed.Count, Is.EqualTo(1));
    }

    [UpstreamTest("DI-003 should fire dialogclosed when dialog is dismissed")]
    public async Task ShouldFireDialogclosedWhenDialogIsDismissed(IPage page)
    {
        var closedPromise = Once<IDialog>(h => page.DialogClosed += h, h => page.DialogClosed -= h);
        page.Dialog += (_, dialog) => IgnoreErrors(dialog.DismissAsync());
        await page.EvaluateAsync("() => confirm('boolean?')");
        var dialog = await closedPromise;
        Assert.That(dialog.Type, Is.EqualTo("confirm"));
        Assert.That(dialog.Message, Is.EqualTo("boolean?"));
    }

    [UpstreamTest("DI-004 should fire dialogclosed for auto-dismissed dialogs")]
    public async Task ShouldFireDialogclosedForAutoDismissedDialogs(IPage page)
    {
        var closedPromise = Once<IDialog>(h => page.DialogClosed += h, h => page.DialogClosed -= h);
        await page.EvaluateAsync("() => alert('yo')");
        var dialog = await closedPromise;
        Assert.That(dialog.Message, Is.EqualTo("yo"));
    }

    [UpstreamTest("DI-005 should allow accepting prompts @smoke")]
    public async Task ShouldAllowAcceptingPrompts(IPage page, bool isElectron)
    {
        It.Skip(isElectron, "prompt() is not a thing in electron");

        page.Dialog += (_, dialog) =>
        {
            Assert.That(dialog.Type, Is.EqualTo("prompt"));
            Assert.That(dialog.DefaultValue, Is.EqualTo("yes."));
            Assert.That(dialog.Message, Is.EqualTo("question?"));
            IgnoreErrors(dialog.AcceptAsync("answer!"));
        };
        var result = await page.EvaluateAsync<string>("() => prompt('question?', 'yes.')");
        Assert.That(result, Is.EqualTo("answer!"));
    }

    [UpstreamTest("DI-006 should dismiss the prompt")]
    public async Task ShouldDismissThePrompt(IPage page, bool isElectron)
    {
        It.Skip(isElectron, "prompt() is not a thing in electron");

        page.Dialog += (_, dialog) => dialog.DismissAsync();
        var result = await page.EvaluateAsync<string>("() => prompt('question?')");
        Assert.That(result, Is.Null);
    }

    [UpstreamTest("DI-007 should accept the confirm prompt")]
    public async Task ShouldAcceptTheConfirmPrompt(IPage page)
    {
        page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync());
        };
        var result = await page.EvaluateAsync<bool>("() => confirm('boolean?')");
        Assert.That(result, Is.EqualTo(true));
    }

    [UpstreamTest("DI-008 should dismiss the confirm prompt")]
    public async Task ShouldDismissTheConfirmPrompt(IPage page)
    {
        page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.DismissAsync());
        };
        var result = await page.EvaluateAsync<bool>("() => confirm('boolean?')");
        Assert.That(result, Is.EqualTo(false));
    }

    [UpstreamTest("DI-009 should be able to close context with open alert")]
    public async Task ShouldBeAbleToCloseContextWithOpenAlert(IPage page)
    {
        var alertPromise = Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h);
        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => alert('hello'), 0);
  }");
        await alertPromise;
    }

    [UpstreamTest("DI-010 should handle multiple alerts")]
    public async Task ShouldHandleMultipleAlerts(IPage page)
    {
        page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync());
        };
        await page.SetContentAsync(@"
    <p>Hello World</p>
    <script>
      alert('Please dismiss this dialog');
      alert('Please dismiss this dialog');
      alert('Please dismiss this dialog');
    </script>
  ");
        Assert.That(await page.TextContentAsync("p"), Is.EqualTo("Hello World"));
    }

    [UpstreamTest("DI-011 should handle multiple confirms")]
    public async Task ShouldHandleMultipleConfirms(IPage page)
    {
        page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync());
        };
        await page.SetContentAsync(@"
    <p>Hello World</p>
    <script>
      confirm('Please confirm me?');
      confirm('Please confirm me?');
      confirm('Please confirm me?');
    </script>
  ");
        Assert.That(await page.TextContentAsync("p"), Is.EqualTo("Hello World"));
    }

    [UpstreamTest("DI-012 should auto-dismiss the prompt without listeners")]
    public async Task ShouldAutoDismissThePromptWithoutListeners(IPage page, bool isElectron)
    {
        It.Skip(isElectron, "prompt() is not a thing in electron");

        var result = await page.EvaluateAsync<string>("() => prompt('question?')");
        Assert.That(result, Is.Null);
    }

    [UpstreamTest("DI-013 should auto-dismiss the alert without listeners")]
    public async Task ShouldAutoDismissTheAlertWithoutListeners(IPage page)
    {
        await page.SetContentAsync("<div onclick=\"window.alert(123); window._clicked=true\">Click me</div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }
}
