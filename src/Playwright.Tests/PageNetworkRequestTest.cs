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
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

///<playwright-file>network-request.spec.ts</playwright-file>
public class PageNetworkRequestTest : PageTestEx
{
    [PlaywrightTest("page-network-request.spec.ts", "should work for main frame navigation request")]
    public async Task ShouldWorkForMainFrameNavigationRequests()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(requests, Has.Count.EqualTo(1));
        Assert.AreEqual(Page.MainFrame, requests[0].Frame);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should work for subframe navigation request")]
    public async Task ShouldWorkForSubframeNavigationRequest()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);

        await Page.GotoAsync(Server.EmptyPage);

        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        Assert.AreEqual(2, requests.Count);
        Assert.AreEqual(Page.FirstChildFrame(), requests[1].Frame);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should work for fetch requests")]
    public async Task ShouldWorkForFetchRequests()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.EvaluateAsync("fetch('/digits/1.png')");
        Assert.AreEqual(1, requests.Where(r => !r.Url.Contains("favicon")).Count());
        Assert.AreEqual(Page.MainFrame, requests[0].Frame);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should return headers")]
    public async Task ShouldReturnHeaders()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);

        string expected = TestConstants.BrowserName switch
        {
            "chromium" => "Chrome",
            "firefox" => "Firefox",
            "webkit" => "WebKit",
            _ => "None"
        };

        StringAssert.Contains(expected, response.Request.Headers["user-agent"]);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should return postData")]
    public async Task ShouldReturnPostData()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/post", _ => Task.CompletedTask);
        IRequest request = null;
        Page.Request += (_, e) => request = e;
        await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
        Assert.NotNull(request);
        Assert.AreEqual("{\"foo\":\"bar\"}", request.PostData);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should work with binary post data")]
    public async Task ShouldWorkWithBinaryPostData()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/post", _ => Task.CompletedTask);
        IRequest request = null;
        Page.Request += (_, e) => request = e;
        await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
        Assert.NotNull(request);
        byte[] data = request.PostDataBuffer;
        Assert.AreEqual(256, data.Length);

        for (int index = 0; index < data.Length; index++)
        {
            Assert.AreEqual(index, data[index]);
        }
    }

    [PlaywrightTest("page-network-request.spec.ts", "should work with binary post data and interception")]
    public async Task ShouldWorkWithBinaryPostDataAndInterception()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/post", _ => Task.CompletedTask);
        await Page.RouteAsync("/post", (route) => route.ContinueAsync());
        IRequest request = null;
        Page.Request += (_, e) => request = e;
        await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
        Assert.NotNull(request);
        byte[] data = request.PostDataBuffer;
        Assert.AreEqual(256, data.Length);

        for (int index = 0; index < data.Length; index++)
        {
            Assert.AreEqual(index, data[index]);
        }
    }

    [PlaywrightTest("page-network-request.spec.ts", "should be |undefined| when there is no post data")]
    public async Task ShouldBeUndefinedWhenThereIsNoPostData()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.Null(response.Request.PostData);
    }


    [PlaywrightTest("page-network-request.spec.ts", "should parse the json post data")]
    public async Task ShouldParseTheJsonPostData()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/post", _ => Task.CompletedTask);
        IRequest request = null;
        Page.Request += (_, e) => request = e;
        await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
        Assert.NotNull(request);
        Assert.AreEqual("bar", request.PostDataJSON()?.GetProperty("foo").ToString());
    }

    [PlaywrightTest("page-network-request.spec.ts", "should parse the data if content-type is application/x-www-form-urlencoded")]
    public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencoded()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/post", _ => Task.CompletedTask);
        IRequest request = null;
        Page.Request += (_, e) => request = e;
        await Page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
        await Page.ClickAsync("input[type=submit]");

        Assert.NotNull(request);
        var element = request.PostDataJSON();
        Assert.AreEqual("bar", element?.GetProperty("foo").ToString());
        Assert.AreEqual("123", element?.GetProperty("baz").ToString());
    }

    [PlaywrightTest("page-network-request.spec.ts", "should parse the data if content-type is application/x-www-form-urlencoded; charset=UTF-8")]
    public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencodedCharsetUTF8()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var requestPromise = Page.WaitForRequestAsync("**/post");
        await Page.EvaluateAsync(@"() => fetch('./post', {
        method: 'POST',
        headers: {
            'content-type': 'application/x-www-form-urlencoded; charset=UTF-8',
        },
        body: 'foo=bar&baz=123'
    })");
        var request = await requestPromise;
        Assert.AreEqual("bar", request.PostDataJSON()?.GetProperty("foo").ToString());
        Assert.AreEqual("123", request.PostDataJSON()?.GetProperty("baz").ToString());
    }

    [PlaywrightTest("page-network-request.spec.ts", "should be |undefined| when there is no post data")]
    public async Task ShouldBeUndefinedWhenThereIsNoPostData2()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.Null(response.Request.PostDataJSON());
    }

    [PlaywrightTest("page-network-request.spec.ts", "should return event source")]
    public async Task ShouldReturnEventSource()
    {
        const string sseMessage = "{\"foo\": \"bar\"}";

        Server.SetRoute("/sse", async ctx =>
        {
            ctx.Response.Headers["content-type"] = "text/event-stream";
            ctx.Response.Headers["connection"] = "keep-alive";
            ctx.Response.Headers["cache-control"] = "no-cache";

            await ctx.Response.Body.FlushAsync();
            await ctx.Response.WriteAsync($"data: {sseMessage}\r\r");
            await ctx.Response.Body.FlushAsync();
        });

        await Page.GotoAsync(Server.EmptyPage);
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);

        Assert.AreEqual(sseMessage, await Page.EvaluateAsync<string>(@"() => {
                const eventSource = new EventSource('/sse');
                return new Promise(resolve => {
                    eventSource.onmessage = e => resolve(e.data);
                });
            }"));

        Assert.AreEqual("eventsource", requests[0].ResourceType);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should return navigation bit")]
    public async Task ShouldReturnNavigationBit()
    {
        var requests = new Dictionary<string, IRequest>();
        Page.Request += (_, e) => requests[e.Url.Split('/').Last()] = e;
        Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
        await Page.GotoAsync(Server.Prefix + "/rrredirect");
        Assert.True(requests["rrredirect"].IsNavigationRequest);
        Assert.True(requests["one-frame.html"].IsNavigationRequest);
        Assert.True(requests["frame.html"].IsNavigationRequest);
        Assert.False(requests["script.js"].IsNavigationRequest);
        Assert.False(requests["style.css"].IsNavigationRequest);
    }

    [PlaywrightTest("page-network-request.spec.ts", "Request.isNavigationRequest", "should return navigation bit when navigating to image")]
    public async Task ShouldReturnNavigationBitWhenNavigatingToImage()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.GotoAsync(Server.Prefix + "/pptr.png");
        Assert.True(requests[0].IsNavigationRequest);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should set bodySize and headersSize")]
    public async Task ShouldSetBodySizeAndHeaderSize()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var tsc = new TaskCompletionSource<RequestSizesResult>();
        Page.Request += async (sender, r) =>
        {
            await (await r.ResponseAsync()).FinishedAsync();
            tsc.SetResult(await r.SizesAsync());
        };
        await Page.EvaluateAsync("() => fetch('./get', { method: 'POST', body: '12345'}).then(r => r.text())");

        var sizes = await tsc.Task;
        Assert.AreEqual(5, sizes.RequestBodySize);
        Assert.GreaterOrEqual(sizes.RequestHeadersSize, 200);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should should set bodySize to 0 if there was no body")]
    public async Task ShouldSetBodysizeTo0IfThereWasNoBody()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var tsc = new TaskCompletionSource<RequestSizesResult>();
        Page.Request += async (sender, r) =>
        {
            await (await r.ResponseAsync()).FinishedAsync();
            tsc.SetResult(await r.SizesAsync());
        };
        await Page.EvaluateAsync("() => fetch('./get').then(r => r.text())");

        var sizes = await tsc.Task;
        Assert.AreEqual(0, sizes.RequestBodySize);
        Assert.GreaterOrEqual(sizes.RequestHeadersSize, 200);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should should set bodySize, headersSize, and transferSize")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Chromium, SkipAttribute.Targets.Webkit)]
    public async Task ShouldSetAllTheResponseSizes()
    {
        Server.SetRoute("/get", async ctx =>
        {
            ctx.Response.Headers["Content-Type"] = "text/plain; charset=utf-8";
            await ctx.Response.WriteAsync("abc134");
            await ctx.Response.CompleteAsync();
        });

        await Page.GotoAsync(Server.EmptyPage);
        var tsc = new TaskCompletionSource<RequestSizesResult>();
        Page.Request += async (sender, r) =>
        {
            await (await r.ResponseAsync()).FinishedAsync();
            tsc.SetResult(await r.SizesAsync());
        };
        var message = await Page.EvaluateAsync<string>("() => fetch('./get').then(r => r.arrayBuffer()).then(b => b.bufferLength)");
        var sizes = await tsc.Task;

        Assert.AreEqual(6, sizes.ResponseBodySize);
        Assert.GreaterOrEqual(sizes.ResponseHeadersSize, 142);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should should set bodySize to 0 when there was no response body")]
    public async Task ShouldSetBodySizeTo0WhenNoResponseBody()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);
        await response.FinishedAsync();

        var sizes = await response.Request.SizesAsync();

        Assert.AreEqual(0, sizes.ResponseBodySize);
        Assert.GreaterOrEqual(sizes.ResponseHeadersSize, 133);
    }

    [PlaywrightTest("page-network-request.spec.ts", "should report raw headers")]
    public async Task ShouldReportRawHeaders()
    {
        var expectedHeaders = new List<Header>();
        Server.SetRoute("/headers", async ctx =>
        {
            foreach (var header in ctx.Request.Headers)
            {
                expectedHeaders.Add(new() { Name = header.Key, Value = header.Value });
            }
            if (BrowserName == "webkit" && TestConstants.IsWindows)
            {
                expectedHeaders = expectedHeaders.Where(h => h.Name.ToLowerInvariant() != "accept-encoding").ToList();
                // Convert "value": "en-US, en-US" => "en-US"
                expectedHeaders = expectedHeaders.Select(e =>
                {
                    if (!e.Name.Equals("accept-language", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return e;
                    }
                    var values = e.Value.Split(',').Select(v => v.Trim()).ToArray();
                    if (values.Length == 1)
                    {
                        return e;
                    }
                    if (values[0] != values[1])
                    {
                        return e;
                    }
                    return new Header { Name = e.Name, Value = values[0] };
                }).ToList();
            }
            expectedHeaders = expectedHeaders.Where(h => h.Name.ToLowerInvariant() != "priority").ToList();

            await ctx.Response.CompleteAsync();
        });

        await Page.GotoAsync(Server.EmptyPage);
        var requestTask = Page.WaitForRequestAsync("**/*");
        await Page.EvaluateAsync(@"async () =>
            await fetch('/headers', {
                headers: [
                    ['header-a', 'value-a'],
                    ['header-b', 'value-b'],
                    ['header-a', 'value-a-1'],
                    ['header-a', 'value-a-2'],
                ]
            })
        ");
        var request = await requestTask;
        var headers = (await request.HeadersArrayAsync()).ToList();
        {
            // Kestrel normalizes the Connection header to lowercase, so we do the same.
            expectedHeaders = expectedHeaders.Select(e => e.Name.ToLowerInvariant() == "connection" ? new Header { Name = "connection", Value = e.Value.ToLowerInvariant() } : e).ToList();
            headers = headers.Select(e => e.Name.ToLowerInvariant() == "connection" ? new Header { Name = "connection", Value = e.Value.ToLowerInvariant() } : e).ToList();
        }
        {
            expectedHeaders.Sort((a, b) => a.Name.CompareTo(b.Name));
            headers.Sort((a, b) => a.Name.CompareTo(b.Name));
        }
        Assert.AreEqual(JsonSerializer.Serialize(expectedHeaders), JsonSerializer.Serialize(headers));
        Assert.AreEqual("value-a, value-a-1, value-a-2", await request.HeaderValueAsync("header-a"));
        Assert.IsNull(await request.HeaderValueAsync("not-there"));
    }

    [PlaywrightTest("page-network-request.spec.ts", "should not allow to access frame on popup main request")]
    public async Task ShouldNotAllowToAccessFrameOnPopupMainRequest()
    {

        await Page.SetContentAsync("<a target=_blank href=\"" + Server.EmptyPage + "\">click me</a>");
        var requestPromise = new TaskCompletionSource<IRequest>();
        Context.Request += (_, e) => requestPromise.SetResult(e);
        var popupPromise = Page.Context.WaitForPageAsync();
        var clicked = Page.GetByText("click me").ClickAsync();
        var request = await requestPromise.Task;

        Assert.True(request.IsNavigationRequest);

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
        {
            var frame = request.Frame;
            return Task.CompletedTask;
        });
        Assert.True(exception.Message.Contains("Frame for this navigation request is not available"));

        var response = await request.ResponseAsync();
        await response.FinishedAsync();
        await popupPromise;
        await clicked;
    }
}
