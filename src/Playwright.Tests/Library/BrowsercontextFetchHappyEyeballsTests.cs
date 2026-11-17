/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-fetch-happy-eyeballs.spec.ts.</summary>
public class BrowsercontextFetchHappyEyeballsTests : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Mode != "default");

    // .NET: __testHookLookup, the DNS lookup hook of the Node.js client, and interceptedHostnameLookup, the variable that it sets,
    // are left out: the .NET API does not have the option, and the tests that pass it are not ported.

    [SetUp]
    public void BeforeEach()
    {
        // .NET: interceptedHostnameLookup = undefined is left out with the variable.
        // Force a new connection every time, so that we can intercept the hostname lookup.
        Server.SetExtraHeaders("/simple.json", new()
        {
            ["Connection"] = "close",
        });
    }

    [UpstreamTest("FE-188 should work with ip6 and port as the host")]
    public async Task ShouldWorkWithIp6AndPortAsTheHost()
    {
        var request = await RequestFixture();
        Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INSIDE_DOCKER")), "docker does not support IPv6 by default");
        var response = await request.GetAsync($"http://[::1]:{Server.Port}/simple.json");
        Assert.That(response.Url, Is.EqualTo($"http://[::1]:{Server.Port}/simple.json"));
        await Expect(response).ToBeOKAsync();
    }
}
