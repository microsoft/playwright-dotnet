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

/// <summary>Port of tests/page/page-click-react.spec.ts.</summary>
public class PageClickReactTests : UpstreamPageTest
{
    [UpstreamTest("CL-106 should timeout when click opens alert")]
    public async Task ShouldTimeoutWhenClickOpensAlert()
    {
        var dialogPromise = Once<IDialog>(h => Page.Dialog += h, h => Page.Dialog -= h);
        await Page.SetContentAsync("<div onclick='window.alert(123)'>Click me</div>");
        var error = await Catch(() => Page.ClickAsync("div", new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded."));
        var dialog = await dialogPromise;
        await dialog.DismissAsync();
    }

    [UpstreamTest("CL-107 should not retarget when element changes on hover")]
    public async Task ShouldNotRetargetWhenElementChangesOnHover()
    {
        await Page.GotoAsync(Server.Prefix + "/react.html");
        await Page.EvaluateAsync(@"() => {
    renderComponent(e('div', {}, [e(MyButton, { name: 'button1', renameOnHover: true }), e(MyButton, { name: 'button2' })]));
  }");
        await Page.ClickAsync("text=button1");
        Assert.That(await Page.EvaluateAsync<bool>("window.button1"), Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<object>("window.button2"), Is.Null);
    }

    [UpstreamTest("CL-108 should not retarget when element is recycled on hover")]
    public async Task ShouldNotRetargetWhenElementIsRecycledOnHover()
    {
        await Page.GotoAsync(Server.Prefix + "/react.html");
        await Page.EvaluateAsync(@"() => {
    function shuffle() {
      renderComponent(e('div', {}, [e(MyButton, { name: 'button2' }), e(MyButton, { name: 'button1' })]));
    }
    renderComponent(e('div', {}, [e(MyButton, { name: 'button1', onHover: shuffle }), e(MyButton, { name: 'button2' })]));
  }");
        await Page.ClickAsync("text=button1");
        Assert.That(await Page.EvaluateAsync<object>("window.button1"), Is.Null);
        Assert.That(await Page.EvaluateAsync<bool>("window.button2"), Is.EqualTo(true));
    }
}
