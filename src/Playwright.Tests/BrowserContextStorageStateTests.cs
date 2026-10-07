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

using System.Text.Json;

namespace Microsoft.Playwright.Tests;

public sealed class BrowserContextStorageStateTests : PageTestEx
{
    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should capture local storage")]
    public async Task ShouldCaptureLocalStorage()
    {
        var page1 = await Context.NewPageAsync();
        await page1.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });

        await page1.GotoAsync("https://www.example.com");
        await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
            }");
        await page1.GotoAsync("https://www.domain.com");
        await page1.EvaluateAsync(@"() =>
            {
                localStorage['name2'] = 'value2';
            }");

        string storage = await Context.StorageStateAsync();

        // TODO: think about IVT-in the StorageState and serializing
        string expected = @"{""cookies"":[],""origins"":[{""origin"":""https://www.domain.com"",""localStorage"":[{""name"":""name2"",""value"":""value2""}]},{""origin"":""https://www.example.com"",""localStorage"":[{""name"":""name1"",""value"":""value1""}]}]}";
        Assert.AreEqual(expected, storage);
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should set local storage")]
    public async Task ShouldSetLocalStorage()
    {
        var context = await Browser.NewContextAsync(new()
        {
            StorageState = "{\"cookies\":[],\"origins\":[{\"origin\":\"https://www.example.com\",\"localStorage\":[{\"name\":\"name1\",\"value\":\"value1\"}]}]}",
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });
        await page.GotoAsync("https://www.example.com");
        var localStorage = await page.EvaluateAsync<string[]>("Object.keys(window.localStorage)");
        Assert.AreEqual(localStorage, new string[] { "name1" });
        var name1Value = await page.EvaluateAsync<string>("window.localStorage.getItem('name1')");
        Assert.AreEqual(name1Value, "value1");
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should round-trip through the file")]
    public async Task ShouldRoundTripThroughTheFile()
    {
        var page1 = await Context.NewPageAsync();
        await page1.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });

        await page1.GotoAsync("https://www.example.com");
        await page1.EvaluateAsync(@"async () =>
            {
                localStorage['name1'] = 'value1';
                document.cookie = 'username=John Doe';

                await new Promise((resolve, reject) => {
                  const openRequest = indexedDB.open('db', 42);
                  openRequest.onupgradeneeded = () => {
                    openRequest.result.createObjectStore('store');
                  };
                  openRequest.onsuccess = () => {
                    const request = openRequest.result.transaction('store', 'readwrite')
                        .objectStore('store')
                        .put('foo', 'bar');
                    request.addEventListener('success', resolve);
                    request.addEventListener('error', reject);
                  };
                });

                return document.cookie;
            }");
        using var tempDir = new TempDirectory();
        string path = Path.Combine(tempDir.Path, "storage-state.json");
        string storage = await Context.StorageStateAsync(new() { IndexedDB = true, Path = path });
        Assert.AreEqual(storage, File.ReadAllText(path));

        await using var context = await Browser.NewContextAsync(new() { StorageStatePath = path });
        var page2 = await context.NewPageAsync();
        await page2.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });

        await page2.GotoAsync("https://www.example.com");
        Assert.AreEqual("value1", await page2.EvaluateAsync<string>("localStorage['name1']"));
        Assert.AreEqual("username=John Doe", await page2.EvaluateAsync<string>("document.cookie"));

        var idbValue = await page2.EvaluateAsync<string>(@"
            () => {
              return new Promise((resolve, reject) => {
                const openRequest = indexedDB.open('db', 42);
                openRequest.addEventListener('success', () => {
                  const db = openRequest.result;
                  const transaction = db.transaction('store', 'readonly');
                  const getRequest = transaction.objectStore('store').get('bar');
                  getRequest.addEventListener('success', () => resolve(getRequest.result));
                  getRequest.addEventListener('error', () => reject(getRequest.error));
                });
                openRequest.addEventListener('error', () => reject(openRequest.error));
              });
            }");
        Assert.AreEqual("foo", idbValue);
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should round-trip OPFS")]
    [Skip(SkipAttribute.Targets.Webkit)] // OPFS is unavailable in non-persistent WebKit contexts
    public async Task ShouldRoundTripOPFS()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"async () => {
            const root = await navigator.storage.getDirectory();
            const nested = await root.getDirectoryHandle('nested', { create: true });
            await nested.getDirectoryHandle('empty', { create: true });

            const binary = await nested.getFileHandle('data.bin', { create: true });
            const binaryWritable = await binary.createWritable();
            await binaryWritable.write(new Uint8Array([0, 1, 2, 255]));
            await binaryWritable.close();

            const text = await root.getFileHandle('hello.txt', { create: true });
            const textWritable = await text.createWritable();
            await textWritable.write('Hello, world!');
            await textWritable.close();
        }");

        Assert.AreEqual("{\"cookies\":[],\"origins\":[]}", await Context.StorageStateAsync());

        using var tempDir = new TempDirectory();
        string path = Path.Combine(tempDir.Path, "storage-state.json");
        string storageState = await Context.StorageStateAsync(new() { Opfs = true, Path = path });
        var origins = JsonDocument.Parse(storageState).RootElement.GetProperty("origins");
        Assert.AreEqual(1, origins.GetArrayLength());
        Assert.AreEqual(Server.Prefix, origins[0].GetProperty("origin").GetString());
        var opfs = origins[0].GetProperty("opfs").EnumerateArray().Select(entry => (entry.GetProperty("path").GetString(), entry.GetProperty("type").GetString(), entry.TryGetProperty("base64", out var base64) ? base64.GetString() : null)).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            ("hello.txt", "file", "SGVsbG8sIHdvcmxkIQ=="),
            ("nested", "directory", null),
            ("nested/data.bin", "file", "AAEC/w=="),
            ("nested/empty", "directory", null),
        }, opfs);
        Assert.AreEqual(storageState, File.ReadAllText(path));
        Assert.AreEqual(storageState, await Context.APIRequest.StorageStateAsync(new() { Opfs = true }));

        async Task CheckContext(IBrowserContext context)
        {
            Assert.AreEqual(storageState, await context.StorageStateAsync(new() { Opfs = true }));
            var checkPage = await context.NewPageAsync();
            await checkPage.GotoAsync(Server.EmptyPage);
            var result = await checkPage.EvaluateAsync<JsonElement>(@"async () => {
                const root = await navigator.storage.getDirectory();
                const hello = await (await root.getFileHandle('hello.txt')).getFile();
                const nested = await root.getDirectoryHandle('nested');
                const data = await (await nested.getFileHandle('data.bin')).getFile();
                const empty = await nested.getDirectoryHandle('empty');
                const emptyEntries = [];
                for await (const name of empty.keys())
                    emptyEntries.push(name);
                return {
                    text: await hello.text(),
                    bytes: [...new Uint8Array(await data.arrayBuffer())],
                    empty: emptyEntries,
                };
            }");
            Assert.AreEqual("Hello, world!", result.GetProperty("text").GetString());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 255 }, result.GetProperty("bytes").EnumerateArray().Select(e => e.GetInt32()).ToArray());
            Assert.AreEqual(0, result.GetProperty("empty").GetArrayLength());
        }

        await using var context2 = await Browser.NewContextAsync(new() { StorageStatePath = path });
        await CheckContext(context2);

        await using var context3 = await Browser.NewContextAsync();
        var page3 = await context3.NewPageAsync();
        await page3.GotoAsync(Server.EmptyPage);
        await page3.EvaluateAsync(@"async () => {
            const root = await navigator.storage.getDirectory();
            await root.getFileHandle('stale.txt', { create: true });
        }");
        await context3.SetStorageStateAsync(path);
        await CheckContext(context3);
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should capture cookies")]
    public async Task ShouldCaptureCookies()
    {
        Server.SetRoute("/setcookie.html", context =>
        {
            context.Response.Cookies.Append("a", "b");
            context.Response.Cookies.Append("empty", "");
            return Task.CompletedTask;
        });

        await Page.GotoAsync(Server.Prefix + "/setcookie.html");
        CollectionAssert.AreEqual(new[] { "a=b", "empty=" }, await Page.EvaluateAsync<string[]>(@"() =>
            {
                const cookies = document.cookie.split(';');
                return cookies.map(cookie => cookie.trim()).sort();
            }"));

        var storageState = await Context.StorageStateAsync();
        StringAssert.Contains(@"""name"":""a"",""value"":""b""", storageState);
        StringAssert.Contains(@"""name"":""empty"",""value"":""""", storageState);
        StringAssert.DoesNotContain(@"""url"":null", storageState);

        await using var context2 = await Browser.NewContextAsync(new() { StorageState = storageState });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        CollectionAssert.AreEqual(new[] { "a=b", "empty=" }, await page2.EvaluateAsync<string[]>(@"() =>
            {
                const cookies = document.cookie.split(';');
                return cookies.map(cookie => cookie.trim()).sort();
            }"));
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should serialize storageState with lone surrogates")]
    public async Task ShouldSerializeStorageStateWithLoneSurrogates()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"chars => window.localStorage.setItem('foo', String.fromCharCode(55934))");
        string storageState = await Context.StorageStateAsync();
        // It should get replaced by the utf8 replacement char (U+FFFD)
        StringAssert.Contains(@"""value"":""\uFFFD""", storageState);
    }

    [PlaywrightTest("browsercontext-storage-state.spec.ts", "should set local storage via setStorageState")]
    public async Task ShouldSetLocalStorageViaSetStorageState()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Body = "<html></html>" });
        });
        await page.GotoAsync("https://www.example.com");
        var localStorage = await page.EvaluateAsync<string>("window.localStorage.getItem('name1')");
        Assert.IsNull(localStorage);

        using var tempDir = new TempDirectory();
        string path = Path.Combine(tempDir.Path, "storage-state.json");
        File.WriteAllText(path, @"{""cookies"":[],""origins"":[{""origin"":""https://www.example.com"",""localStorage"":[{""name"":""name1"",""value"":""value1""}]}]}");
        await context.SetStorageStateAsync(path);

        await page.GotoAsync("https://www.example.com");
        localStorage = await page.EvaluateAsync<string>("window.localStorage.getItem('name1')");
        Assert.AreEqual("value1", localStorage);
    }
}
