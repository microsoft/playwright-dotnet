/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-cache-storage.spec.ts.</summary>
public class PageCacheStorageTests : UpstreamPageTest
{
    [UpstreamTest("ST-032 CacheStorage entry should survive page.reload()")]
    public async Task CacheStorageEntryShouldSurvivePageReload()
    {
        Info.Fixme(BrowserName == "webkit", "Ephemeral CacheStorage is not persisted across reload in WebKit, consistent with Safari");
        Info.Skip(IsAndroid, "not supported on Android");

        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"async () => {
    const cache = await caches.open('repro-cache');
    await cache.put('/meta', new Response('payload'));
  }");

        await Page.ReloadAsync();

        var after = await Page.EvaluateAsync<string>(@"async () => {
    const cache = await caches.open('repro-cache');
    const resp = await cache.match('/meta');
    return resp ? await resp.text() : null;
  }");
        Assert.That(after, Is.EqualTo("payload"));
    }
}
