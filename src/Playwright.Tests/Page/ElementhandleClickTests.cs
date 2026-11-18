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

/// <summary>Port of tests/page/elementhandle-click.spec.ts.</summary>
public class ElementhandleClickTests : UpstreamPageTest
{
    [UpstreamTest("EH-010 should work @smoke")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("EH-011 should work with Node removed")]
    public async Task ShouldWorkWithNodeRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = await Page.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("EH-012 should work for Shadow DOM v1")]
    public async Task ShouldWorkForShadowDOMV1()
    {
        await Page.GotoAsync(Server.Prefix + "/shadow.html");
        var buttonHandle = (IElementHandle)await Page.EvaluateHandleAsync("() => window['button']");
        await buttonHandle.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("EH-013 should work for TextNodes")]
    public async Task ShouldWorkForTextNodes()
    {
        await Page.SetContentAsync(@"
    <div id='outer' onclick=""window['result'] = 'Clicked ' + event.target.id;"">
      <div id='inner' style=""max-width: 50px"">Lorem ipsum dolor sit amet consectetur adipiscing elit proin, integer curabitur imperdiet rhoncus cursus tincidunt bibendum, consequat sed magnis laoreet luctus mollis tellus. Nisl parturient mus accumsan feugiat sem laoreet magnis nisi, aptent per sollicitudin gravida orci ac blandit, viverra eros praesent auctor vivamus semper bibendum. Consequat sed habitasse luctus dictumst gravida platea semper phasellus, nascetur ridiculus purus est varius quisque et scelerisque, id vehicula eleifend montes sollicitudin dis velit. Pellentesque ridiculus per natoque et eleifend taciti nunc, laoreet auctor at condimentum imperdiet ante, conubia mi cubilia scelerisque sociosqu sem.</div>
      Custom Text.
    </div>
  ");
        var buttonTextNode = (IElementHandle)await Page.EvaluateHandleAsync("() => document.querySelector('#outer').lastChild");
        await buttonTextNode.ClickAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked outer"));
    }

    [UpstreamTest("EH-014 should throw for detached nodes")]
    public async Task ShouldThrowForDetachedNodes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("button => button.remove()", button);
        Exception error = null;
        error = await Catch(() => button.ClickAsync());
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("EH-015 should throw for hidden nodes with force")]
    public async Task ShouldThrowForHiddenNodesWithForce()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("button => button.style.display = 'none'", button);
        var error = await Catch(() => button.ClickAsync(new() { Force = true }));
        Assert.That(error.Message, Does.Contain("Element is not visible"));
    }

    [UpstreamTest("EH-016 should throw for recursively hidden nodes with force")]
    public async Task ShouldThrowForRecursivelyHiddenNodesWithForce()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("button => button.parentElement.style.display = 'none'", button);
        var error = await Catch(() => button.ClickAsync(new() { Force = true }));
        Assert.That(error.Message, Does.Contain("Element is not visible"));
    }

    [UpstreamTest("EH-017 should throw for <br> elements with force")]
    public async Task ShouldThrowForBrElementsWithForce()
    {
        await Page.SetContentAsync("hello<br>goodbye");
        var br = await Page.QuerySelectorAsync("br");
        var error = await Catch(() => br.ClickAsync(new() { Force = true }));
        Assert.That(error.Message, Does.Contain("Element is outside of the viewport"));
    }

    [UpstreamTest("EH-018 should double click the button")]
    public async Task ShouldDoubleClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
    window['double'] = false;
    const button = document.querySelector('button');
    button.addEventListener('dblclick', event => {
      window['double'] = true;
    });
  }");
        var button = await Page.QuerySelectorAsync("button");
        await button.DblClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("double"), Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }
}
