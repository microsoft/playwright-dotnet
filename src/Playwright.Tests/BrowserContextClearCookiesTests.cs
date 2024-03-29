/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class BrowserContextClearCookiesTests : PageTestEx
{
    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should clear cookies")]
    public async Task ShouldClearCookies()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Context.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie1",
                    Value = "1"
                }
            });
        Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
        await Context.ClearCookiesAsync();
        Assert.IsEmpty(await Context.CookiesAsync());
        await Page.ReloadAsync();
        Assert.IsEmpty(await Page.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should isolate cookies when clearing")]
    public async Task ShouldIsolateWhenClearing()
    {
        await using var anotherContext = await Browser.NewContextAsync();
        await Context.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Name = "page1cookie",
                    Value = "page1value",
                    Url = Server.EmptyPage
                }
            });

        await anotherContext.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Name = "page2cookie",
                    Value = "page2value",
                    Url = Server.EmptyPage
                }
            });

        Assert.That(await Context.CookiesAsync(), Has.Count.EqualTo(1));
        Assert.That(await anotherContext.CookiesAsync(), Has.Count.EqualTo(1));

        await Context.ClearCookiesAsync();
        Assert.IsEmpty((await Context.CookiesAsync()));
        Assert.That((await anotherContext.CookiesAsync()), Has.Count.EqualTo(1));

        await anotherContext.ClearCookiesAsync();
        Assert.IsEmpty(await Context.CookiesAsync());
        Assert.IsEmpty(await anotherContext.CookiesAsync());
    }

    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should remove cookies by name")]
    public async Task ShouldRemoveCookiesByName()
    {
        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            }
        ]);
        await Page.GotoAsync(Server.Prefix);
        Assert.AreEqual("cookie1=1; cookie2=2", await Page.EvaluateAsync<string>("document.cookie"));
        await Context.ClearCookiesAsync(new() { Name = "cookie1" });
        Assert.AreEqual("cookie2=2", await Page.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should remove cookies by name regex")]
    public async Task ShouldRemoveCookiesByNameRegex()
    {
        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            }
        ]);
        await Page.GotoAsync(Server.Prefix);
        Assert.AreEqual("cookie1=1; cookie2=2", await Page.EvaluateAsync<string>("document.cookie"));
        await Context.ClearCookiesAsync(new() { NameRegex = new Regex("coo.*1") });
        Assert.AreEqual("cookie2=2", await Page.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should remove cookies by domain")]
    public async Task ShouldRemoveCookiesByDomain()
    {
        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(Server.CrossProcessPrefix).Host,
                Path = "/"
            }
        ]);
        await Page.GotoAsync(Server.Prefix);
        Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
        await Page.GotoAsync(Server.CrossProcessPrefix);
        Assert.AreEqual("cookie2=2", await Page.EvaluateAsync<string>("document.cookie"));
        await Context.ClearCookiesAsync(new() { Domain = new Uri(Server.CrossProcessPrefix).Host });
        Assert.IsEmpty(await Page.EvaluateAsync<string>("document.cookie"));
        await Page.GotoAsync(Server.Prefix);
        Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should remove cookies by path")]
    public async Task ShouldRemoveCookiesByPath()
    {
        await Context.AddCookiesAsync(
        [
        new Cookie
        {
            Name = "cookie1",
            Value = "1",
            Domain = new Uri(Server.Prefix).Host,
            Path = "/api/v1"
        },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/api/v2"
            },
            new Cookie
            {
                Name = "cookie3",
                Value = "3",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            }
    ]);
        await Page.GotoAsync(Server.Prefix + "/api/v1");
        Assert.AreEqual("cookie1=1; cookie3=3", await Page.EvaluateAsync<string>("document.cookie"));
        await Context.ClearCookiesAsync(new() { Path = "/api/v1" });
        Assert.AreEqual("cookie3=3", await Page.EvaluateAsync<string>("document.cookie"));
        await Page.GotoAsync(Server.Prefix + "/api/v2");
        Assert.AreEqual("cookie2=2; cookie3=3", await Page.EvaluateAsync<string>("document.cookie"));
        await Page.GotoAsync(Server.Prefix + "/");
        Assert.AreEqual("cookie3=3", await Page.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should remove cookies by name and domain")]
    public async Task ShouldRemoveCookiesByNameAndDomain()
    {
        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(Server.Prefix).Host,
                Path = "/"
            },
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(Server.CrossProcessPrefix).Host,
                Path = "/"
            }
        ]);
        await Page.GotoAsync(Server.Prefix);
        Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
        await Context.ClearCookiesAsync(new() { Name = "cookie1", Domain = new Uri(Server.Prefix).Host });
        Assert.IsEmpty(await Page.EvaluateAsync<string>("document.cookie"));
        await Page.GotoAsync(Server.CrossProcessPrefix);
        Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
    }
}
