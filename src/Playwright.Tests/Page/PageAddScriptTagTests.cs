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

/// <summary>Port of tests/page/page-add-script-tag.spec.ts.</summary>
public class PageAddScriptTagTests
{
    [UpstreamTest("AT-002 should work with a url")]
    public async Task ShouldWorkWithAUrl(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var scriptHandle = await page.AddScriptTagAsync(new() { Url = "/injectedfile.js" });
        Assert.That(scriptHandle.AsElement(), Is.Not.Null);
        Assert.That(await page.EvaluateAsync<int>("() => window['__injected']"), Is.EqualTo(42));
    }

    [UpstreamTest("AT-003 should work with a url and type=module")]
    public async Task ShouldWorkWithAUrlAndTypeModule(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.AddScriptTagAsync(new() { Url = "/es6/es6import.js", Type = "module" });
        Assert.That(await page.EvaluateAsync<int>("() => window['__es6injected']"), Is.EqualTo(42));
    }

    [UpstreamTest("AT-004 should work with a path and type=module")]
    public async Task ShouldWorkWithAPathAndTypeModule(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.AddScriptTagAsync(new() { Path = asset("es6/es6pathimport.js"), Type = "module" });
        await page.WaitForFunctionAsync("window.__es6injected");
        Assert.That(await page.EvaluateAsync<int>("() => window['__es6injected']"), Is.EqualTo(42));
    }

    [UpstreamTest("AT-005 should work with a content and type=module")]
    public async Task ShouldWorkWithAContentAndTypeModule(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.AddScriptTagAsync(new() { Content = "import num from '/es6/es6module.js';window.__es6injected = num;", Type = "module" });
        await page.WaitForFunctionAsync("window.__es6injected");
        Assert.That(await page.EvaluateAsync<int>("() => window['__es6injected']"), Is.EqualTo(42));
    }

    [UpstreamTest("AT-006 should throw an error if loading from url fail")]
    public async Task ShouldThrowAnErrorIfLoadingFromUrlFail(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Exception error = null;
        try
        {
            await page.AddScriptTagAsync(new() { Url = "/nonexistfile.js" });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("AT-007 should work with a path")]
    public async Task ShouldWorkWithAPath(IPage page, Server server, Func<string, string> asset)
    {
        await page.GotoAsync(server.EmptyPage);
        var scriptHandle = await page.AddScriptTagAsync(new() { Path = asset("injectedfile.js") });
        Assert.That(scriptHandle.AsElement(), Is.Not.Null);
        Assert.That(await page.EvaluateAsync<int>("() => window['__injected']"), Is.EqualTo(42));
    }

    [UpstreamTest("AT-008 should include sourceURL when path is provided")]
    public async Task ShouldIncludeSourceURLWhenPathIsProvided(IPage page, Server server, string browserName, Func<string, string> asset)
    {
        It.Skip(browserName == "webkit");

        await page.GotoAsync(server.EmptyPage);
        await page.AddScriptTagAsync(new() { Path = asset("injectedfile.js") });
        var result = await page.EvaluateAsync<string>("() => window['__injectedError'].stack");
        Assert.That(result, Does.Contain(Path.Combine("assets", "injectedfile.js")));
    }

    [UpstreamTest("AT-009 should work with content")]
    public async Task ShouldWorkWithContent(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var scriptHandle = await page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 35;" });
        Assert.That(scriptHandle.AsElement(), Is.Not.Null);
        Assert.That(await page.EvaluateAsync<int>("() => window['__injected']"), Is.EqualTo(35));
    }

    [UpstreamTest("AT-010 should throw when added with content to the CSP page")]
    public async Task ShouldThrowWhenAddedWithContentToTheCSPPage(IPage page, Server server)
    {
        // Firefox fires onload for blocked script before it issues the CSP console error.
        await page.GotoAsync(server.Prefix + "/csp.html");
        Exception error = null;
        error = await Catch(() => page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 35;" }));
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("AT-011 should throw when added with URL to the CSP page")]
    public async Task ShouldThrowWhenAddedWithURLToTheCSPPage(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid, "No cross-process on Android");

        await page.GotoAsync(server.Prefix + "/csp.html");
        Exception error = null;
        error = await Catch(() => page.AddScriptTagAsync(new() { Url = server.CrossProcessPrefix + "/injectedfile.js" }));
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("AT-012 should throw a nice error when the request fails")]
    public async Task ShouldThrowANiceErrorWhenTheRequestFails(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var url = server.Prefix + "/this_does_not_exist.js";
        var error = await Catch(() => page.AddScriptTagAsync(new() { Url = url }));
        Assert.That(error.Message, Does.Contain(url));
    }
}
