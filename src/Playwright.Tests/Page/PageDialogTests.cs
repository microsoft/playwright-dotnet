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
public class PageDialogTests : UpstreamPageTest
{
    [UpstreamTest("DI-001 should fire")]
    public async Task ShouldFire()
    {
        Page.Dialog += (_, dialog) =>
        {
            Assert.That(dialog.Type, Is.EqualTo("alert"));
            Assert.That(dialog.DefaultValue, Is.EqualTo(""));
            Assert.That(dialog.Message, Is.EqualTo("yo"));
            IgnoreErrors(dialog.AcceptAsync());
        };
        await Page.EvaluateAsync("() => alert('yo')");
    }

    [UpstreamTest("DI-002 should fire dialogclosed when dialog is accepted")]
    public async Task ShouldFireDialogclosedWhenDialogIsAccepted()
    {
        var closed = new List<IDialog>();
        Page.DialogClosed += (_, dialog) => closed.Add(dialog);
        IDialog opened = null;
        Page.Dialog += (_, dialog) =>
        {
            opened = dialog;
            IgnoreErrors(dialog.AcceptAsync());
        };
        await Page.EvaluateAsync("() => alert('yo')");
        await Expect(() => Assert.That(closed.Count, Is.EqualTo(1))).ToPassAsync();
        Assert.That(closed[0], Is.SameAs(opened));
        // Perform some roundtrips to ensure the event does not fire twice.
        await Page.EvaluateAsync("() => 1");
        await Page.EvaluateAsync("() => 1");
        Assert.That(closed.Count, Is.EqualTo(1));
    }

    [UpstreamTest("DI-003 should fire dialogclosed when dialog is dismissed")]
    public async Task ShouldFireDialogclosedWhenDialogIsDismissed()
    {
        var closedPromise = Once<IDialog>(h => Page.DialogClosed += h, h => Page.DialogClosed -= h);
        Page.Dialog += (_, dialog) => IgnoreErrors(dialog.DismissAsync());
        await Page.EvaluateAsync("() => confirm('boolean?')");
        var dialog = await closedPromise;
        Assert.That(dialog.Type, Is.EqualTo("confirm"));
        Assert.That(dialog.Message, Is.EqualTo("boolean?"));
    }

    [UpstreamTest("DI-004 should fire dialogclosed for auto-dismissed dialogs")]
    public async Task ShouldFireDialogclosedForAutoDismissedDialogs()
    {
        var closedPromise = Once<IDialog>(h => Page.DialogClosed += h, h => Page.DialogClosed -= h);
        await Page.EvaluateAsync("() => alert('yo')");
        var dialog = await closedPromise;
        Assert.That(dialog.Message, Is.EqualTo("yo"));
    }

    [UpstreamTest("DI-005 should allow accepting prompts @smoke")]
    public async Task ShouldAllowAcceptingPrompts()
    {
        Info.Skip(IsElectron, "prompt() is not a thing in electron");

        Page.Dialog += (_, dialog) =>
        {
            Assert.That(dialog.Type, Is.EqualTo("prompt"));
            Assert.That(dialog.DefaultValue, Is.EqualTo("yes."));
            Assert.That(dialog.Message, Is.EqualTo("question?"));
            IgnoreErrors(dialog.AcceptAsync("answer!"));
        };
        var result = await Page.EvaluateAsync<string>("() => prompt('question?', 'yes.')");
        Assert.That(result, Is.EqualTo("answer!"));
    }

    [UpstreamTest("DI-006 should dismiss the prompt")]
    public async Task ShouldDismissThePrompt()
    {
        Info.Skip(IsElectron, "prompt() is not a thing in electron");

        Page.Dialog += (_, dialog) => dialog.DismissAsync();
        var result = await Page.EvaluateAsync<string>("() => prompt('question?')");
        Assert.That(result, Is.Null);
    }

    [UpstreamTest("DI-007 should accept the confirm prompt")]
    public async Task ShouldAcceptTheConfirmPrompt()
    {
        Page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync());
        };
        var result = await Page.EvaluateAsync<bool>("() => confirm('boolean?')");
        Assert.That(result, Is.EqualTo(true));
    }

    [UpstreamTest("DI-008 should dismiss the confirm prompt")]
    public async Task ShouldDismissTheConfirmPrompt()
    {
        Page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.DismissAsync());
        };
        var result = await Page.EvaluateAsync<bool>("() => confirm('boolean?')");
        Assert.That(result, Is.EqualTo(false));
    }

    [UpstreamTest("DI-009 should be able to close context with open alert")]
    public async Task ShouldBeAbleToCloseContextWithOpenAlert()
    {
        var alertPromise = Once<IDialog>(h => Page.Dialog += h, h => Page.Dialog -= h);
        await Page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => alert('hello'), 0);
  }");
        await alertPromise;
    }

    [UpstreamTest("DI-010 should handle multiple alerts")]
    public async Task ShouldHandleMultipleAlerts()
    {
        Page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync());
        };
        await Page.SetContentAsync(@"
    <p>Hello World</p>
    <script>
      alert('Please dismiss this dialog');
      alert('Please dismiss this dialog');
      alert('Please dismiss this dialog');
    </script>
  ");
        Assert.That(await Page.TextContentAsync("p"), Is.EqualTo("Hello World"));
    }

    [UpstreamTest("DI-011 should handle multiple confirms")]
    public async Task ShouldHandleMultipleConfirms()
    {
        Page.Dialog += (_, dialog) =>
        {
            IgnoreErrors(dialog.AcceptAsync());
        };
        await Page.SetContentAsync(@"
    <p>Hello World</p>
    <script>
      confirm('Please confirm me?');
      confirm('Please confirm me?');
      confirm('Please confirm me?');
    </script>
  ");
        Assert.That(await Page.TextContentAsync("p"), Is.EqualTo("Hello World"));
    }

    [UpstreamTest("DI-012 should auto-dismiss the prompt without listeners")]
    public async Task ShouldAutoDismissThePromptWithoutListeners()
    {
        Info.Skip(IsElectron, "prompt() is not a thing in electron");

        var result = await Page.EvaluateAsync<string>("() => prompt('question?')");
        Assert.That(result, Is.Null);
    }

    [UpstreamTest("DI-013 should auto-dismiss the alert without listeners")]
    public async Task ShouldAutoDismissTheAlertWithoutListeners()
    {
        await Page.SetContentAsync("<div onclick=\"window.alert(123); window._clicked=true\">Click me</div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }
}
