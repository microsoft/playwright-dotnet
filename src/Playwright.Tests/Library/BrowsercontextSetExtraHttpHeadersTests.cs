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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-set-extra-http-headers.spec.ts.</summary>
public class BrowsercontextSetExtraHttpHeadersTests
{
    [UpstreamTest("NE-119 should override extra headers from browser context")]
    public async Task ShouldOverrideExtraHeadersFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            ExtraHTTPHeaders = new Dictionary<string, string> { ["fOo"] = "bAr", ["baR"] = "foO" },
        });
        var page = await context.NewPageAsync();
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Foo"] = "Bar",
        });
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        await context.CloseAsync();
        Assert.That(request.Headers["foo"], Is.EqualTo("Bar"));
        Assert.That(request.Headers["bar"], Is.EqualTo("foO"));
    }

    [UpstreamTest("NE-120 should throw for non-string header values")]
    [LocalFixme("dotnet: the client does not check the values of ExtraHTTPHeaders: a null value goes to the driver, and the message is 'extraHTTPHeaders[0].value: expected string, got undefined'. Upstream expects 'Expected value of header \"foo\" to be String, but \"object\" is found.'")]
    public async Task ShouldThrowForNonStringHeaderValues(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var error3 = await Catch(() => browser.NewContextAsync(new(contextOptions) { ExtraHTTPHeaders = new Dictionary<string, string> { ["foo"] = null } }));
        Assert.That(error3.Message, Does.Contain("Expected value of header \"foo\" to be String, but \"object\" is found."));
    }
}
