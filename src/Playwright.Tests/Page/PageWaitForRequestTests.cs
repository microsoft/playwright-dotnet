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

/// <summary>Port of tests/page/page-wait-for-request.spec.ts.</summary>
public class PageWaitForRequestTests : UpstreamPageTest
{
    [UpstreamTest("WA-121 should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(Server.Prefix + "/digits/2.png"),
            Page.EvaluateAsync(@"() => {
      void fetch('/digits/1.png');
      void fetch('/digits/2.png');
      void fetch('/digits/3.png');
    }"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/digits/2.png"));
    }

    [UpstreamTest("WA-122 should work with predicate")]
    public async Task ShouldWorkWithPredicate()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(request => request.Url == Server.Prefix + "/digits/2.png"),
            Page.EvaluateAsync(@"() => {
      void fetch('/digits/1.png');
      void fetch('/digits/2.png');
      void fetch('/digits/3.png');
    }"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/digits/2.png"));
    }

    [UpstreamTest("WA-123 should respect timeout")]
    [LocalFixme("dotnet: the message is 'Timeout 1ms exceeded while waiting for event \"Request\"', upstream expects 'event \"request\"'")]
    public async Task ShouldRespectTimeout()
    {
        Exception error = null;
        error = await Catch(() => Page.WaitForRequestAsync(_ => false, new() { Timeout = 1 }));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
        Assert.That(error.Message, Does.Contain("Timeout 1ms exceeded while waiting for event \"request\""));
        // Error stack should point to the api call.
        // .NET: left out, a .NET stack trace has the frames of the library:
        // const firstFrame = error.stack.split('\n').find(line => line.startsWith('    at '));
        // expect(firstFrame).toContain(__filename);
    }

    [UpstreamTest("WA-124 should respect default timeout")]
    public async Task ShouldRespectDefaultTimeout()
    {
        Exception error = null;
        Page.SetDefaultTimeout(1);
        error = await Catch(() => Page.WaitForRequestAsync(_ => false));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-125 should log the url")]
    [LocalFixme("dotnet: WaitForRequestAsync(url) does not log 'waiting for request \"<url>\"' on its waiter as upstream does, the message is only 'Timeout 1000ms exceeded while waiting for event \"Request\"'")]
    public async Task ShouldLogTheUrl()
    {
        var error = await Catch(() => Page.WaitForRequestAsync("long-long-long-long-long-long-long-long-long-long-long-long-long-long.css", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("waiting for request \"long-long-long-long-long-long-long-long-long-long…\""));
    }

    [UpstreamTest("WA-126 should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(Server.Prefix + "/digits/2.png", new() { Timeout = 0 }),
            Page.EvaluateAsync(@"() => window.builtins.setTimeout(() => {
      void fetch('/digits/1.png');
      void fetch('/digits/2.png');
      void fetch('/digits/3.png');
    }, 50)"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/digits/2.png"));
    }

    [UpstreamTest("WA-127 should work with url match")]
    public async Task ShouldWorkWithUrlMatch()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(new Regex(@"digits\/\d\.png")),
            Page.EvaluateAsync(@"() => {
      void fetch('/digits/1.png');
    }"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/digits/1.png"));
    }
}
