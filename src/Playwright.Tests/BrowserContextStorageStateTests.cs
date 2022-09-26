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
        string expected = @"{""cookies"":[],""origins"":[{""origin"":""https://www.example.com"",""localStorage"":[{""name"":""name1"",""value"":""value1""}]},{""origin"":""https://www.domain.com"",""localStorage"":[{""name"":""name2"",""value"":""value2""}]}]}";
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
        await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
                document.cookie = 'username=John Doe';
            }");
        using var tempDir = new TempDirectory();
        string path = Path.Combine(tempDir.Path, "storage-state.json");
        string storage = await Context.StorageStateAsync(new() { Path = path });
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
        if (TestConstants.IsWebKit || TestConstants.IsFirefox)
        {
            StringAssert.Contains(@"""sameSite"":""None""", storageState);
        }
        else
        {
            StringAssert.Contains(@"""sameSite"":""Lax""", storageState);
        }
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
}
