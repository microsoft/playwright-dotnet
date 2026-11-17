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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-localstorage.spec.ts.</summary>
public class PageLocalstorageTests
{
    [BeforeEach]
    public static void Skip(bool isElectron) => Info.Skip(isElectron, "Electron shares a single context between tests");

    [UpstreamTest("ST-023 localStorage.items returns empty array on fresh origin")]
    public async Task LocalStorageItemsReturnsEmptyArrayOnFreshOrigin(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        ExpectToEqual(await page.LocalStorage.ItemsAsync(), Array.Empty<object>());
    }

    [UpstreamTest("ST-024 localStorage.getItem returns null for missing key")]
    public async Task LocalStorageGetItemReturnsNullForMissingKey(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.LocalStorage.GetItemAsync("absent"), Is.Null);
    }

    [UpstreamTest("ST-025 localStorage.setItem persists and surfaces in items()/getItem()")]
    public async Task LocalStorageSetItemPersistsAndSurfacesInItemsGetItem(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.LocalStorage.SetItemAsync("alpha", "1");
        await page.LocalStorage.SetItemAsync("beta", "2");

        ExpectToEqual(new HashSet<WebStorageItem>(await page.LocalStorage.ItemsAsync()), new HashSet<object>
        {
            new { name = "alpha", value = "1" },
            new { name = "beta", value = "2" },
        });
        Assert.That(await page.LocalStorage.GetItemAsync("alpha"), Is.EqualTo("1"));
        Assert.That(await page.EvaluateAsync<string>("() => localStorage.getItem('alpha')"), Is.EqualTo("1"));
    }

    [UpstreamTest("ST-026 localStorage.setItem overwrites existing value")]
    public async Task LocalStorageSetItemOverwritesExistingValue(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.LocalStorage.SetItemAsync("k", "first");
        await page.LocalStorage.SetItemAsync("k", "second");
        Assert.That(await page.LocalStorage.GetItemAsync("k"), Is.EqualTo("second"));
    }

    [UpstreamTest("ST-027 localStorage.removeItem removes a single item")]
    public async Task LocalStorageRemoveItemRemovesASingleItem(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.LocalStorage.SetItemAsync("a", "1");
        await page.LocalStorage.SetItemAsync("b", "2");

        await page.LocalStorage.RemoveItemAsync("a");
        ExpectToEqual(await page.LocalStorage.ItemsAsync(), new[] { new { name = "b", value = "2" } });
    }

    [UpstreamTest("ST-028 localStorage.clear empties storage")]
    public async Task LocalStorageClearEmptiesStorage(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.LocalStorage.SetItemAsync("a", "1");
        await page.LocalStorage.SetItemAsync("b", "2");

        await page.LocalStorage.ClearAsync();
        ExpectToEqual(await page.LocalStorage.ItemsAsync(), Array.Empty<object>());
    }

    [UpstreamTest("ST-029 sessionStorage round-trip")]
    public async Task SessionStorageRoundTrip(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        ExpectToEqual(await page.SessionStorage.ItemsAsync(), Array.Empty<object>());

        await page.SessionStorage.SetItemAsync("s1", "v1");
        await page.SessionStorage.SetItemAsync("s2", "v2");
        ExpectToEqual(new HashSet<WebStorageItem>(await page.SessionStorage.ItemsAsync()), new HashSet<object>
        {
            new { name = "s1", value = "v1" },
            new { name = "s2", value = "v2" },
        });
        Assert.That(await page.SessionStorage.GetItemAsync("s1"), Is.EqualTo("v1"));

        await page.SessionStorage.RemoveItemAsync("s1");
        ExpectToEqual(await page.SessionStorage.ItemsAsync(), new[] { new { name = "s2", value = "v2" } });

        await page.SessionStorage.ClearAsync();
        ExpectToEqual(await page.SessionStorage.ItemsAsync(), Array.Empty<object>());
    }

    [UpstreamTest("ST-030 localStorage and sessionStorage are independent")]
    public async Task LocalStorageAndSessionStorageAreIndependent(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.LocalStorage.SetItemAsync("shared", "local");
        await page.SessionStorage.SetItemAsync("shared", "session");

        Assert.That(await page.LocalStorage.GetItemAsync("shared"), Is.EqualTo("local"));
        Assert.That(await page.SessionStorage.GetItemAsync("shared"), Is.EqualTo("session"));

        await page.LocalStorage.ClearAsync();
        ExpectToEqual(await page.LocalStorage.ItemsAsync(), Array.Empty<object>());
        Assert.That(await page.SessionStorage.GetItemAsync("shared"), Is.EqualTo("session"));
    }

    [UpstreamTest("ST-031 storage methods are scoped to the current origin")]
    public async Task StorageMethodsAreScopedToTheCurrentOrigin(IPage page, Server server, bool isAndroid)
    {
        Info.Skip(isAndroid, "these are same origins on Android due to our test server limitation");

        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.LocalStorage.SetItemAsync("k", "origin-1");

        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        ExpectToEqual(await page.LocalStorage.ItemsAsync(), Array.Empty<object>());
        await page.LocalStorage.SetItemAsync("k", "origin-2");

        await page.GotoAsync(server.Prefix + "/empty.html");
        Assert.That(await page.LocalStorage.GetItemAsync("k"), Is.EqualTo("origin-1"));
    }
}
