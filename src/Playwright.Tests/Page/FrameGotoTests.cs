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

using System.Globalization;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/frame-goto.spec.ts.</summary>
public class FrameGotoTests
{
    [UpstreamTest("FR-020 should navigate subframes @smoke")]
    public async Task ShouldNavigateSubframes(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        Assert.That(page.Frames[0].Url, Does.Contain("/frames/one-frame.html"));
        Assert.That(page.Frames[1].Url, Does.Contain("/frames/frame.html"));

        var response = await page.Frames[1].GotoAsync(server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Frame, Is.SameAs(page.Frames[1]));
    }

    [UpstreamTest("FR-021 should reject when frame detaches")]
    public async Task ShouldRejectWhenFrameDetaches(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");

        server.SetRoute("/one-style.css", (req, res) => { });
        var navigationPromise = Catch(() => page.Frames[1].GotoAsync(server.Prefix + "/one-style.html"));
        await server.WaitForRequest("/one-style.css");

        await page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
        var error = await navigationPromise;
        if (browserName == "chromium")
        {
            Assert.That(error.Message.Contains("net::ERR_ABORTED", StringComparison.Ordinal) || error.Message.ToLowerInvariant().Contains("frame was detached", StringComparison.Ordinal), Is.EqualTo(true));
        }
        else
        {
            Assert.That(error.Message.ToLowerInvariant(), Does.Contain("frame was detached"));
        }
    }

    [UpstreamTest("FR-022 should continue after client redirect")]
    [Fixme("dotnet: the message is 'Timeout 5000ms exceeded.', without the 'page.goto: ' prefix of the call", Browsers = "chromium,webkit")]
    public async Task ShouldContinueAfterClientRedirect(IPage page, Server server, bool isAndroid, string browserName, bool isBidi)
    {
        It.Fixme(isAndroid);
        It.Fixme(browserName == "firefox" && !isBidi, "script.js is requested before navigationCommitted arrives");

        server.SetRoute("/frames/script.js", (req, res) => { });
        var url = server.Prefix + "/frames/child-redirect.html";
        var error = await Catch(() => page.GotoAsync(url, new() { Timeout = 5000, WaitUntil = WaitUntilState.NetworkIdle }));
        Assert.That(error.Message, Does.Contain("page.goto: Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain($"navigating to \"{url}\", waiting until \"networkidle\""));
    }

    [UpstreamTest("FR-023 should return matching responses")]
    public async Task ShouldReturnMatchingResponses(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        // Attach three frames.
        var frames = new[]
        {
            await AttachFrame(page, "frame1", server.EmptyPage),
            await AttachFrame(page, "frame2", server.EmptyPage),
            await AttachFrame(page, "frame3", server.EmptyPage),
        };
        var serverResponses = new List<ServerResponse>();
        server.SetRoute("/0.html", (req, res) => serverResponses.Add(res));
        server.SetRoute("/1.html", (req, res) => serverResponses.Add(res));
        server.SetRoute("/2.html", (req, res) => serverResponses.Add(res));
        var navigations = new List<Task<IResponse>>();
        for (var i = 0; i < 3; ++i)
        {
            navigations.Add(frames[i].GotoAsync(server.Prefix + "/" + i.ToString(CultureInfo.InvariantCulture) + ".html"));
            await server.WaitForRequest("/" + i.ToString(CultureInfo.InvariantCulture) + ".html");
        }
        // Respond from server out-of-order.
        var serverResponseTexts = new[] { "AAA", "BBB", "CCC" };
        foreach (var i in new[] { 1, 2, 0 })
        {
            serverResponses[i].End(serverResponseTexts[i]);
            var response = await navigations[i];
            Assert.That(response.Frame, Is.SameAs(frames[i]));
            Assert.That(await response.TextAsync(), Is.EqualTo(serverResponseTexts[i]));
        }
    }
}
