/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

namespace Microsoft.Playwright.Tests.Locator;


public class LocatorFrameTests : PageTestEx
{
    public async Task RouteIFrame(IPage page)
    {
        await page.RouteAsync("**/empty.html", async route =>
        {
            await route.FulfillAsync(new()
            {
                Body = "<iframe src=\"/iframe.html\"></iframe>",
                ContentType = "text/html"
            });
        });
        await page.RouteAsync("**/iframe.html", async route =>
        {
            await route.FulfillAsync(new()
            {
                Body = @"
                    <html>
                    <div>
                        <button>Hello iframe</button>
                        <iframe src=""iframe-2.html""></iframe>
                    </div>
                    <span>1</span>
                    <span>2</span>
                    </html>",
                ContentType = "text/html"
            });
        });
        await page.RouteAsync("**/iframe-2.html", async route =>
        {
            await route.FulfillAsync(new()
            {
                Body = "<html><button>Hello nested iframe</button></html>",
                ContentType = "text/html"
            });
        });
    }

    public async Task RouteAmbiguous(IPage page)
    {
        await page.RouteAsync("**/empty.html", async route =>
        {
            await route.FulfillAsync(new()
            {
                Body = @"<iframe src=""iframe-1.html""></iframe>
             <iframe src=""iframe-2.html""></iframe>
             <iframe src=""iframe-3.html""></iframe>",
                ContentType = "text/html"
            });
        });
        await page.RouteAsync("**/iframe-*", async route =>
        {
            var path = new Uri(route.Request.Url).AbsolutePath.AsSpan(1).ToString();
            await route.FulfillAsync(new()
            {
                Body = $"<html><button>Hello from {path}</button></html>",
                ContentType = "text/html"
            });
        });
    }

    [PlaywrightTest("locator-frame.spec.ts", "should work for iframe")]
    public async Task ShouldWorkForIFrame()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.AreEqual(await button.InnerTextAsync(), "Hello iframe");
        await Expect(button).ToHaveTextAsync("Hello iframe");
        await button.ClickAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should work for nested iframe")]
    public async Task ShouldWorkForNestedIFrame()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.AreEqual(await button.InnerTextAsync(), "Hello nested iframe");
        await Expect(button).ToHaveTextAsync("Hello nested iframe");
        await button.ClickAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should work for $ and $$")]
    public async Task ShouldWorkForAnd()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var locator = Page.FrameLocator("iframe").Locator("button");
        await Expect(locator).ToHaveTextAsync("Hello iframe");
        var spans = Page.FrameLocator("iframe").Locator("span");
        await Expect(spans).ToHaveCountAsync(2);
    }

    [PlaywrightTest("locator-frame.spec.ts", "should wait for frame")]
    public async Task ShouldWaitForFrame()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var error = await Page.FrameLocator("iframe").Locator("span").ClickAsync(new() { Timeout = 1000 }).ContinueWith(t => t.Exception.InnerException);
        StringAssert.Contains("waiting for frame \"iframe\"", error.Message);
    }

    [PlaywrightTest("locator-frame.spec.ts", "should wait for frame 2")]
    public async Task ShouldWaitForFrame2()
    {
        await RouteIFrame(Page);
        async void myTask()
        {
            await Task.Delay(300);
            await Page.GotoAsync(Server.EmptyPage);
        }
        myTask();
        await Page.FrameLocator("iframe").Locator("button").ClickAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should wait for frame to go")]
    public async Task ShouldWaitForFrameToGo()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        async void myTask()
        {
            await Task.Delay(300);
            await Page.EvalOnSelectorAsync("iframe", "e => e.remove()");
        }
        myTask();
        await Expect(Page.FrameLocator("iframe").Locator("button")).ToBeHiddenAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should not wait for frame")]
    public async Task ShouldNotWaitForFrame()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator("iframe").Locator("span")).ToBeHiddenAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should not wait for frame 2")]
    public async Task ShouldNotWaitForFrame2()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator("iframe").Locator("span")).Not.ToBeVisibleAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should not wait for frame 3")]
    public async Task ShouldNotWaitForFrame3()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator("iframe").Locator("span")).ToHaveCountAsync(0);
    }

    [PlaywrightTest("locator-frame.spec.ts", "should click in lazy iframe")]
    public async Task ShouldClickInLazyIframe()
    {
        await Page.RouteAsync("**/iframe.html", async route =>
        {
            await route.FulfillAsync(new()
            {
                Body = "<html><button>Hello iframe</button></html>",
                ContentType = "text/html"
            });
        });

        // empty pge
        await Page.GotoAsync(Server.EmptyPage);

        // add blank iframe
        async void myTask()
        {
            await Task.Delay(500);
            await Page.EvaluateAsync(@"() => {
                    const iframe = document.createElement('iframe');
                    document.body.appendChild(iframe);
                }");
            // navigate iframe
            async void myTask2()
            {
                await Task.Delay(500);
                await Page.EvaluateAsync(@"() => document.querySelector('iframe').src = 'iframe.html'");
            }
            myTask2();
        }
        myTask();

        // Click in iframe
        var button = Page.FrameLocator("iframe").Locator("button");
        var (_, text, _) = await TaskUtils.WhenAll(
            button.ClickAsync().WithBooleanReturnType(),
            button.InnerTextAsync(),
            Expect(button).ToHaveTextAsync("Hello iframe").WithBooleanReturnType()
        );
        Assert.AreEqual("Hello iframe", text);
    }


    [PlaywrightTest("locator-frame.spec.ts", "waitFor should survive frame reattach")]
    public async Task WaitForShouldSurviveFrameReattach()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").Locator("button:has-text(\"Hello nested iframe\")");
        var promise = button.WaitForAsync();
        await Page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await Page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'iframe-2.html';
    document.body.appendChild(iframe);
    }");
        await promise;
    }

    [PlaywrightTest("locator-frame.spec.ts", "click should survive frame reattach")]
    public async Task ClickShouldSurviveFrameReattach()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").Locator("button:has-text(\"Hello nested iframe\")");
        var promise = button.ClickAsync();
        await Page.Locator("iframe").EvaluateAsync("e => e.remove()");
        await Page.EvaluateAsync(@"() => {
                const iframe = document.createElement('iframe');
                iframe.src = 'iframe-2.html';
                document.body.appendChild(iframe);
            }");
        await promise;
    }

    [PlaywrightTest("locator-frame.spec.ts", "should work survive iframe navigation")]
    public async Task ShouldWorkSurviveIFrameNavigation()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").Locator("button:has-text(\"Hello nested iframe\")");
        var promise = button.ClickAsync();
        await Page.Locator("iframe").EvaluateAsync("e => e.src = 'iframe-2.html'");
        await promise;
    }

    [PlaywrightTest("locator-frame.spec.ts", "should not work for non-frame")]
    public async Task ShouldNotWorkForNonFrame()
    {
        await RouteIFrame(Page);
        await Page.SetContentAsync("<div></div>");
        var button = Page.FrameLocator("div").Locator("button");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => button.WaitForAsync());
        StringAssert.Contains("<div></div>", error.Message);
        StringAssert.Contains("<iframe> was expected", error.Message);
    }

    [PlaywrightTest("locator-frame.spec.ts", "locator.frameLocator should work for iframe")]
    public async Task LocatorFrameLocatorShouldWorkForIFrame()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.Locator("body").FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.AreEqual(await button.InnerTextAsync(), "Hello iframe");
        await Expect(button).ToHaveTextAsync("Hello iframe");
        await button.ClickAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "locator.frameLocator should throw on ambiguity")]
    public async Task LocatorFrameLocatorShouldThrowOnAmbiguity()
    {
        await RouteAmbiguous(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.Locator("body").FrameLocator("iframe").Locator("button");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => button.WaitForAsync());
        StringAssert.Contains("Error: strict mode violation: \"body >> iframe\" resolved to 3 elements", error.Message);
    }

    [PlaywrightTest("locator-frame.spec.ts", "locator.frameLocator should not throw on first/last/nth")]
    public async Task LocatorFrameLocatorShouldNotThrowOnFirstLastNth()
    {
        await RouteAmbiguous(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button1 = Page.Locator("body").FrameLocator("iframe").First.Locator("button");
        await Expect(button1).ToHaveTextAsync("Hello from iframe-1.html");
        var button2 = Page.Locator("body").FrameLocator("iframe").Nth(1).Locator("button");
        await Expect(button2).ToHaveTextAsync("Hello from iframe-2.html");
        var button3 = Page.Locator("body").FrameLocator("iframe").Last.Locator("button");
        await Expect(button3).ToHaveTextAsync("Hello from iframe-3.html");
    }
}
