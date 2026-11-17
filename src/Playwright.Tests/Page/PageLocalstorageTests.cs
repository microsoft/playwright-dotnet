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
public class PageLocalstorageTests : UpstreamPageTest
{
    [SetUp]
    public void Skip() => Info.Skip(IsElectron, "Electron shares a single context between tests");

    [UpstreamTest("ST-023 localStorage.items returns empty array on fresh origin")]
    public async Task LocalStorageItemsReturnsEmptyArrayOnFreshOrigin()
    {
        await Page.GotoAsync(Server.EmptyPage);
        ExpectToEqual(await Page.LocalStorage.ItemsAsync(), Array.Empty<object>());
    }

    [UpstreamTest("ST-024 localStorage.getItem returns null for missing key")]
    public async Task LocalStorageGetItemReturnsNullForMissingKey()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await Page.LocalStorage.GetItemAsync("absent"), Is.Null);
    }

    [UpstreamTest("ST-025 localStorage.setItem persists and surfaces in items()/getItem()")]
    public async Task LocalStorageSetItemPersistsAndSurfacesInItemsGetItem()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("alpha", "1");
        await Page.LocalStorage.SetItemAsync("beta", "2");

        ExpectToEqual(new HashSet<WebStorageItem>(await Page.LocalStorage.ItemsAsync()), new HashSet<object>
        {
            new { name = "alpha", value = "1" },
            new { name = "beta", value = "2" },
        });
        Assert.That(await Page.LocalStorage.GetItemAsync("alpha"), Is.EqualTo("1"));
        Assert.That(await Page.EvaluateAsync<string>("() => localStorage.getItem('alpha')"), Is.EqualTo("1"));
    }

    [UpstreamTest("ST-026 localStorage.setItem overwrites existing value")]
    public async Task LocalStorageSetItemOverwritesExistingValue()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("k", "first");
        await Page.LocalStorage.SetItemAsync("k", "second");
        Assert.That(await Page.LocalStorage.GetItemAsync("k"), Is.EqualTo("second"));
    }

    [UpstreamTest("ST-027 localStorage.removeItem removes a single item")]
    public async Task LocalStorageRemoveItemRemovesASingleItem()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("a", "1");
        await Page.LocalStorage.SetItemAsync("b", "2");

        await Page.LocalStorage.RemoveItemAsync("a");
        ExpectToEqual(await Page.LocalStorage.ItemsAsync(), new[] { new { name = "b", value = "2" } });
    }

    [UpstreamTest("ST-028 localStorage.clear empties storage")]
    public async Task LocalStorageClearEmptiesStorage()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("a", "1");
        await Page.LocalStorage.SetItemAsync("b", "2");

        await Page.LocalStorage.ClearAsync();
        ExpectToEqual(await Page.LocalStorage.ItemsAsync(), Array.Empty<object>());
    }

    [UpstreamTest("ST-029 sessionStorage round-trip")]
    public async Task SessionStorageRoundTrip()
    {
        await Page.GotoAsync(Server.EmptyPage);
        ExpectToEqual(await Page.SessionStorage.ItemsAsync(), Array.Empty<object>());

        await Page.SessionStorage.SetItemAsync("s1", "v1");
        await Page.SessionStorage.SetItemAsync("s2", "v2");
        ExpectToEqual(new HashSet<WebStorageItem>(await Page.SessionStorage.ItemsAsync()), new HashSet<object>
        {
            new { name = "s1", value = "v1" },
            new { name = "s2", value = "v2" },
        });
        Assert.That(await Page.SessionStorage.GetItemAsync("s1"), Is.EqualTo("v1"));

        await Page.SessionStorage.RemoveItemAsync("s1");
        ExpectToEqual(await Page.SessionStorage.ItemsAsync(), new[] { new { name = "s2", value = "v2" } });

        await Page.SessionStorage.ClearAsync();
        ExpectToEqual(await Page.SessionStorage.ItemsAsync(), Array.Empty<object>());
    }

    [UpstreamTest("ST-030 localStorage and sessionStorage are independent")]
    public async Task LocalStorageAndSessionStorageAreIndependent()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("shared", "local");
        await Page.SessionStorage.SetItemAsync("shared", "session");

        Assert.That(await Page.LocalStorage.GetItemAsync("shared"), Is.EqualTo("local"));
        Assert.That(await Page.SessionStorage.GetItemAsync("shared"), Is.EqualTo("session"));

        await Page.LocalStorage.ClearAsync();
        ExpectToEqual(await Page.LocalStorage.ItemsAsync(), Array.Empty<object>());
        Assert.That(await Page.SessionStorage.GetItemAsync("shared"), Is.EqualTo("session"));
    }

    [UpstreamTest("ST-031 storage methods are scoped to the current origin")]
    public async Task StorageMethodsAreScopedToTheCurrentOrigin()
    {
        Info.Skip(IsAndroid, "these are same origins on Android due to our test server limitation");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.LocalStorage.SetItemAsync("k", "origin-1");

        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        ExpectToEqual(await Page.LocalStorage.ItemsAsync(), Array.Empty<object>());
        await Page.LocalStorage.SetItemAsync("k", "origin-2");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(await Page.LocalStorage.GetItemAsync("k"), Is.EqualTo("origin-1"));
    }
}
