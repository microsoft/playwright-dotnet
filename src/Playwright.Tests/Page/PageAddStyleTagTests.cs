/**
 * Copyright 2017 Google Inc. All rights reserved.
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

/// <summary>Port of tests/page/page-add-style-tag.spec.ts.</summary>
public class PageAddStyleTagTests : UpstreamPageTest
{
    [UpstreamTest("AT-014 should work with a url @smoke")]
    public async Task ShouldWorkWithAUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var styleHandle = await Page.AddStyleTagAsync(new() { Url = "/injectedstyle.css" });
        Assert.That(styleHandle.AsElement(), Is.Not.Null);
        Assert.That(await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"), Is.EqualTo("rgb(255, 0, 0)"));
    }

    [UpstreamTest("AT-015 should throw an error if loading from url fail")]
    public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Exception error = null;
        try
        {
            await Page.AddStyleTagAsync(new() { Url = "/nonexistfile.js" });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("AT-016 should work with a path")]
    public async Task ShouldWorkWithAPath()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var styleHandle = await Page.AddStyleTagAsync(new() { Path = Asset("injectedstyle.css") });
        Assert.That(styleHandle.AsElement(), Is.Not.Null);
        Assert.That(await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"), Is.EqualTo("rgb(255, 0, 0)"));
    }

    [UpstreamTest("AT-017 should include sourceURL when path is provided")]
    public async Task ShouldIncludeSourceURLWhenPathIsProvided()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.AddStyleTagAsync(new() { Path = Asset("injectedstyle.css") });
        var styleHandle = await Page.QuerySelectorAsync("style");
        var styleContent = await Page.EvaluateAsync<string>("style => style.innerHTML", styleHandle);
        Assert.That(styleContent, Does.Contain(Path.Combine("assets", "injectedstyle.css")));
    }

    [UpstreamTest("AT-018 should work with content")]
    public async Task ShouldWorkWithContent()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var styleHandle = await Page.AddStyleTagAsync(new() { Content = "body { background-color: green; }" });
        Assert.That(styleHandle.AsElement(), Is.Not.Null);
        Assert.That(await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"), Is.EqualTo("rgb(0, 128, 0)"));
    }

    [UpstreamTest("AT-019 should throw when added with content to the CSP page")]
    public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
    {
        await Page.GotoAsync(Server.Prefix + "/csp.html");
        Exception error = null;
        error = await Catch(() => Page.AddStyleTagAsync(new() { Content = "body { background-color: green; }" }));
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("AT-020 should throw when added with URL to the CSP page")]
    public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        await Page.GotoAsync(Server.Prefix + "/csp.html");
        Exception error = null;
        error = await Catch(() => Page.AddStyleTagAsync(new() { Url = Server.CrossProcessPrefix + "/injectedstyle.css" }));
        Assert.That(error, Is.Not.Null);
    }
}
