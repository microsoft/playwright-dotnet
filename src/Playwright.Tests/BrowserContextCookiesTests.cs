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

namespace Microsoft.Playwright.Tests;

public class BrowserContextCookiesTests : PageTestEx
{
    [PlaywrightTest("browsercontext-cookies.spec.ts", "should return no cookies in pristine browser context")]
    public async Task ShouldReturnNoCookiesInPristineBrowserContext()
        => Assert.IsEmpty(await Context.CookiesAsync());

    [PlaywrightTest("browsercontext-cookies.spec.ts", "should get a cookie")]
    public async Task ShouldGetACookie()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("username=John Doe", await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                return document.cookie;
            }"));

        var cookie = (await Page.Context.CookiesAsync()).Single();
        Assert.AreEqual("username", cookie.Name);
        Assert.AreEqual("John Doe", cookie.Value);
        Assert.AreEqual("localhost", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(-1, cookie.Expires);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);
    }

    [PlaywrightTest("browsercontext-cookies.spec.ts", "should get a non-session cookie")]
    public async Task ShouldGetANonSessionCookie()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var date = new DateTime(2038, 1, 1);
        Assert.AreEqual("username=John Doe", await Page.EvaluateAsync<string>(@"timestamp => {
                const date = new Date(timestamp);
                  document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
                  return document.cookie;
            }", date));

        var cookie = (await Page.Context.CookiesAsync()).Single();
        Assert.AreEqual("username", cookie.Name);
        Assert.AreEqual("John Doe", cookie.Value);
        Assert.AreEqual("localhost", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.NotNull(cookie.Expires);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);
    }

    [PlaywrightTest("browsercontext-cookies.spec.ts", "should properly report httpOnly cookie")]
    public async Task ShouldProperlyReportHttpOnlyCookie()
    {
        Server.SetRoute("/empty.html", context =>
        {
            context.Response.Headers["Set-Cookie"] = "name=value;HttpOnly; Path=/";
            return Task.CompletedTask;
        });
        await Page.GotoAsync(Server.EmptyPage);
        var cookies = await Context.CookiesAsync();
        Assert.That(cookies, Has.Count.EqualTo(1));
        Assert.IsTrue(cookies.ElementAt(0).HttpOnly);
    }

    [PlaywrightTest("browsercontext-cookies.spec.ts", @"should properly report ""Strict"" sameSite cookie")]
    [Skip(SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Windows)]
    public async Task ShouldProperlyReportStrictSameSiteCookie()
    {
        Server.SetRoute("/empty.html", context =>
        {
            context.Response.Headers["Set-Cookie"] = "name=value;SameSite=Strict";
            return Task.CompletedTask;
        });
        await Page.GotoAsync(Server.EmptyPage);
        var cookies = await Context.CookiesAsync();
        Assert.That(cookies, Has.Count.EqualTo(1));
        Assert.AreEqual(SameSiteAttribute.Strict, cookies.ElementAt(0).SameSite);
    }

    [PlaywrightTest("browsercontext-cookies.spec.ts", @"should properly report ""Lax"" sameSite cookie")]
    [Skip(SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Windows)]
    public async Task ShouldProperlyReportLaxSameSiteCookie()
    {
        Server.SetRoute("/empty.html", context =>
        {
            context.Response.Headers["Set-Cookie"] = "name=value;SameSite=Lax";
            return Task.CompletedTask;
        });
        await Page.GotoAsync(Server.EmptyPage);
        var cookies = await Context.CookiesAsync();
        Assert.That(cookies, Has.Count.EqualTo(1));
        Assert.AreEqual(SameSiteAttribute.Lax, cookies.ElementAt(0).SameSite);
    }

    [PlaywrightTest("browsercontext-cookies.spec.ts", "should get multiple cookies")]
    public async Task ShouldGetMultipleCookies()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await Context.CookiesAsync(), Is.Empty);
        string documentCookie = await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                document.cookie = 'password=1234';
                return document.cookie.split('; ').sort().join('; ');
            }");

        var cookies = (await Context.CookiesAsync()).OrderBy(c => c.Name).ToList();
        Assert.AreEqual("password=1234; username=John Doe", documentCookie);
        var cookie = cookies[0];
        Assert.AreEqual("password", cookie.Name);
        Assert.AreEqual("1234", cookie.Value);
        Assert.AreEqual("localhost", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(cookie.Expires, -1);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);

        cookie = cookies[1];
        Assert.AreEqual("username", cookie.Name);
        Assert.AreEqual("John Doe", cookie.Value);
        Assert.AreEqual("localhost", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(cookie.Expires, -1);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);
    }

    [PlaywrightTest("browsercontext-cookies.spec.ts", "should get cookies from multiple urls")]
    public async Task ShouldGetCookiesFromMultipleUrls()
    {
        await Context.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Url = "https://foo.com",
                    Name = "doggo",
                    Value = "woofs"
                },
                new Cookie
                {
                    Url = "https://bar.com",
                    Name = "catto",
                    Value = "purrs"
                },
                new Cookie
                {
                    Url = "https://baz.com",
                    Name = "birdo",
                    Value = "tweets"
                },
            });
        var cookies = (await Context.CookiesAsync(new[] { "https://foo.com", "https://baz.com" })).OrderBy(c => c.Name).ToList();

        Assert.AreEqual(2, cookies.Count);

        var cookie = cookies[0];
        Assert.AreEqual("birdo", cookie.Name);
        Assert.AreEqual("tweets", cookie.Value);
        Assert.AreEqual("baz.com", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(cookie.Expires, -1);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsTrue(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);

        cookie = cookies[1];
        Assert.AreEqual("doggo", cookie.Name);
        Assert.AreEqual("woofs", cookie.Value);
        Assert.AreEqual("foo.com", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(cookie.Expires, -1);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsTrue(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);
    }
}
