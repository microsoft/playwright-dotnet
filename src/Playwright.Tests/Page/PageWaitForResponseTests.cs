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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-response.spec.ts.</summary>
public class PageWaitForResponseTests : UpstreamPageTest
{
    [UpstreamTest("WA-129 should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(Server.Prefix + "/digits/2.png"),
            Page.EvaluateAsync(@"() => {
      void fetch('/digits/1.png');
      void fetch('/digits/2.png');
      void fetch('/digits/3.png');
    }"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/digits/2.png"));
    }

    [UpstreamTest("WA-130 should respect timeout")]
    public async Task ShouldRespectTimeout()
    {
        Exception error = null;
        error = await Catch(() => Page.WaitForResponseAsync(_ => false, new() { Timeout = 1 }));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-131 should respect default timeout")]
    public async Task ShouldRespectDefaultTimeout()
    {
        Exception error = null;
        Page.SetDefaultTimeout(1);
        error = await Catch(() => Page.WaitForResponseAsync(_ => false));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
        // Error stack should point to the api call.
        // .NET: left out, a .NET stack trace has the frames of the library:
        // const firstFrame = error.stack.split('\n').find(line => line.startsWith('    at '));
        // expect(firstFrame).toContain(__filename);
    }

    [UpstreamTest("WA-132 should log the url")]

    [LocalFixme("dotnet: WaitForResponseAsync(url) does not log 'waiting for response \"<url>\"' on its waiter as upstream does, the message is only 'Timeout 1000ms exceeded while waiting for event \"Response\"'")]
    public async Task ShouldLogTheUrl()
    {
        var error1 = await Catch(() => Page.WaitForResponseAsync("foo.css", new() { Timeout = 1000 }));
        Assert.That(error1.Message, Does.Contain("waiting for response \"foo.css\""));
        var error2 = await Catch(() => Page.WaitForResponseAsync(new Regex("foo.css", RegexOptions.IgnoreCase), new() { Timeout = 1000 }));
        Assert.That(error2.Message, Does.Contain("waiting for response /foo.css/i"));
    }

    [UpstreamTest("WA-133 should work with predicate")]
    public async Task ShouldWorkWithPredicate()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(response => response.Url == Server.Prefix + "/digits/2.png"),
            Page.EvaluateAsync(@"() => {
      void fetch('/digits/1.png');
      void fetch('/digits/2.png');
      void fetch('/digits/3.png');
    }"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/digits/2.png"));
    }

    [UpstreamTest("WA-135 sync predicate should be only called once")]
    public async Task SyncPredicateShouldBeOnlyCalledOnce()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var counter = 0;
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(response =>
            {
                ++counter;
                return response.Url == Server.Prefix + "/digits/1.png";
            }),
            Page.EvaluateAsync(@"async () => {
      await fetch('/digits/1.png');
      await fetch('/digits/2.png');
      await fetch('/digits/3.png');
    }"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/digits/1.png"));
        Assert.That(counter, Is.EqualTo(1));
    }

    [UpstreamTest("WA-136 should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(Server.Prefix + "/digits/2.png", new() { Timeout = 0 }),
            Page.EvaluateAsync(@"() => window.builtins.setTimeout(() => {
      void fetch('/digits/1.png');
      void fetch('/digits/2.png');
      void fetch('/digits/3.png');
    }, 50)"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/digits/2.png"));
    }
}
