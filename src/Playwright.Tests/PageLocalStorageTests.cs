/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

///<playwright-file>page-localstorage.spec.ts</playwright-file>
public class PageLocalStorageTests : PageTestEx
{
    [PlaywrightTest("page-localstorage.spec.ts", "localStorage.items returns empty array on fresh origin")]
    public async Task LocalStorageItemsReturnsEmptyArrayOnFreshOrigin()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.IsEmpty(await Page.LocalStorage.ItemsAsync());
    }

    [PlaywrightTest("page-localstorage.spec.ts", "localStorage.getItem returns null for missing key")]
    public async Task LocalStorageGetItemReturnsNullForMissingKey()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.IsNull(await Page.LocalStorage.GetItemAsync("absent"));
    }

    [PlaywrightTest("page-localstorage.spec.ts", "localStorage.setItem persists and surfaces in items()/getItem()")]
    public async Task LocalStorageSetItemPersistsAndSurfacesInItemsGetItem()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("alpha", "1");
        await Page.LocalStorage.SetItemAsync("beta", "2");

        var items = (await Page.LocalStorage.ItemsAsync()).OrderBy(item => item.Name).ToList();
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("alpha", items[0].Name);
        Assert.AreEqual("1", items[0].Value);
        Assert.AreEqual("beta", items[1].Name);
        Assert.AreEqual("2", items[1].Value);
        Assert.AreEqual("1", await Page.LocalStorage.GetItemAsync("alpha"));
        Assert.AreEqual("1", await Page.EvaluateAsync<string>("() => localStorage.getItem('alpha')"));
    }

    [PlaywrightTest("page-localstorage.spec.ts", "localStorage.setItem overwrites existing value")]
    public async Task LocalStorageSetItemOverwritesExistingValue()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("k", "first");
        await Page.LocalStorage.SetItemAsync("k", "second");
        Assert.AreEqual("second", await Page.LocalStorage.GetItemAsync("k"));
    }

    [PlaywrightTest("page-localstorage.spec.ts", "localStorage.removeItem removes a single item")]
    public async Task LocalStorageRemoveItemRemovesASingleItem()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("a", "1");
        await Page.LocalStorage.SetItemAsync("b", "2");

        await Page.LocalStorage.RemoveItemAsync("a");
        var items = await Page.LocalStorage.ItemsAsync();
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("b", items[0].Name);
        Assert.AreEqual("2", items[0].Value);
    }

    [PlaywrightTest("page-localstorage.spec.ts", "localStorage.clear empties storage")]
    public async Task LocalStorageClearEmptiesStorage()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("a", "1");
        await Page.LocalStorage.SetItemAsync("b", "2");

        await Page.LocalStorage.ClearAsync();
        Assert.IsEmpty(await Page.LocalStorage.ItemsAsync());
    }

    [PlaywrightTest("page-localstorage.spec.ts", "sessionStorage round-trip")]
    public async Task SessionStorageRoundTrip()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.IsEmpty(await Page.SessionStorage.ItemsAsync());

        await Page.SessionStorage.SetItemAsync("s1", "v1");
        await Page.SessionStorage.SetItemAsync("s2", "v2");
        var items = (await Page.SessionStorage.ItemsAsync()).OrderBy(item => item.Name).ToList();
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("s1", items[0].Name);
        Assert.AreEqual("v1", items[0].Value);
        Assert.AreEqual("s2", items[1].Name);
        Assert.AreEqual("v2", items[1].Value);
        Assert.AreEqual("v1", await Page.SessionStorage.GetItemAsync("s1"));

        await Page.SessionStorage.RemoveItemAsync("s1");
        items = (await Page.SessionStorage.ItemsAsync()).ToList();
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("s2", items[0].Name);

        await Page.SessionStorage.ClearAsync();
        Assert.IsEmpty(await Page.SessionStorage.ItemsAsync());
    }

    [PlaywrightTest("page-localstorage.spec.ts", "localStorage and sessionStorage are independent")]
    public async Task LocalStorageAndSessionStorageAreIndependent()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.LocalStorage.SetItemAsync("shared", "local");
        await Page.SessionStorage.SetItemAsync("shared", "session");

        Assert.AreEqual("local", await Page.LocalStorage.GetItemAsync("shared"));
        Assert.AreEqual("session", await Page.SessionStorage.GetItemAsync("shared"));

        await Page.LocalStorage.ClearAsync();
        Assert.IsEmpty(await Page.LocalStorage.ItemsAsync());
        Assert.AreEqual("session", await Page.SessionStorage.GetItemAsync("shared"));
    }

    [PlaywrightTest("page-localstorage.spec.ts", "storage methods are scoped to the current origin")]
    public async Task StorageMethodsAreScopedToTheCurrentOrigin()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.LocalStorage.SetItemAsync("k", "origin-1");

        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.IsEmpty(await Page.LocalStorage.ItemsAsync());
        await Page.LocalStorage.SetItemAsync("k", "origin-2");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.AreEqual("origin-1", await Page.LocalStorage.GetItemAsync("k"));
    }
}
