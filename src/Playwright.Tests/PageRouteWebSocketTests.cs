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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class PageRouteWebSocketTests : PageTestEx
{
    private Task SetupWS(IPage page, int port, string path) => SetupWS(page.MainFrame, port, path);

    private async Task SetupWS(IFrame target, int port, string path)
    {
        await target.GotoAsync("about:blank");
        await target.EvaluateAsync(@"({ port, binaryType }) => {
            window.log = [];
            window.ws = new WebSocket('ws://localhost:' + port + '/ws');
            window.ws.binaryType = binaryType;
            window.ws.addEventListener('open', () => window.log.push('open'));
            window.ws.addEventListener('close', event => window.log.push(`close code=${event.code} reason=${event.reason} wasClean=${event.wasClean}`));
            window.ws.addEventListener('error', event => window.log.push(`error`));
            window.ws.addEventListener('message', async event => {
            let data;
            if (typeof event.data === 'string')
                data = event.data;
            else if (event.data instanceof Blob)
                data = 'blob:' + await event.data.text();
            else
                data = 'arraybuffer:' + await (new Blob([event.data])).text();
            window.log.push(`message: data=${data} origin=${event.origin} lastEventId=${event.lastEventId}`);
            });
            window.wsOpened = new Promise(f => window.ws.addEventListener('open', () => f()));
        }", new Dictionary<string, object>
        {
            ["port"] = port,
            ["binaryType"] = path
        });
    }

    async Task AssertAreEqualWithRetriesAsync(Func<Task<string[]>> func, string[] expected)
    {
        var maxTimeout = 5_000;
        var start = DateTime.Now;
        while (true)
        {
            var result = await func();
            if (result.SequenceEqual(expected))
            {
                return;
            }
            Console.WriteLine($"Actual  : {string.Join(", ", result)}");
            Console.WriteLine($"Expected: {string.Join(", ", expected)}");
            if (DateTime.Now - start > TimeSpan.FromMilliseconds(maxTimeout))
            {
                Assert.AreEqual(expected, result);
            }
            await Task.Delay(100);
        }
    }

    [PlaywrightTest("page-route-web-socket.spec.ts", "should work with ws.close")]
    public async Task ShouldworkWithWSClose()
    {
        var tcs = new TaskCompletionSource<IWebSocketRoute>();
        await Page.RouteWebSocketAsync(new Regex($".*"), ws =>
        {
            ws.ConnectToServer();
            tcs.SetResult(ws);
        });

        var wsTask = Server.WaitForWebSocketAsync();
        await SetupWS(Page, Server.Port, "blob");
        var ws = await wsTask;

        var route = await tcs.Task;
        route.Send("hello");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>(@"() => window.log"), new[] { "open", $"message: data=hello origin=ws://localhost:{Server.Port} lastEventId=" });
    }

    [PlaywrightTest("page-route-web-socket.spec.ts", "should pattern match")]
    public async Task ShouldPatternMatch()
    {
        await Page.RouteWebSocketAsync(new Regex(".*\\/ws$"), ws =>
        {
            ws.ConnectToServer();
        });

        await Page.RouteWebSocketAsync("**/mock-ws", ws =>
        {
            ws.OnMessage(message =>
            {
                ws.Send("mock-response");
            });
        });

        var wsPromise = Server.WaitForWebSocketAsync();

        await Page.GotoAsync("about:blank");
        await Page.EvaluateAsync(@"async ({ port }) => {
            window.log = [];
            window.ws1 = new WebSocket('ws://localhost:' + port + '/ws');
            window.ws1.addEventListener('message', event => window.log.push(`ws1:${event.data}`));
            window.ws2 = new WebSocket('ws://localhost:' + port + '/something/something/mock-ws');
            window.ws2.addEventListener('message', event => window.log.push(`ws2:${event.data}`));
            await Promise.all([
                new Promise(f => window.ws1.addEventListener('open', f)),
                new Promise(f => window.ws2.addEventListener('open', f)),
            ]);
        }", new Dictionary<string, object>
        {
            ["port"] = Server.Port
        });

        using var ws = await wsPromise;
        ws.MessageReceived += async (message, _) =>
        {
            await ws.SendAsync("response").ConfigureAwait(false);
        };

        await Page.EvaluateAsync("() => window.ws1.send('request')");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[] { "ws1:response" });

        await Page.EvaluateAsync("() => window.ws2.send('request')");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[] { "ws1:response", "ws2:mock-response" });
    }

    [PlaywrightTest("page-route-web-socket.spec.ts", "should work with server")]
    public async Task ShouldWorkWithServer()
    {
        var tcs = new TaskCompletionSource<IWebSocketRoute>();
        await Page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            var server = ws.ConnectToServer();
            ws.OnMessage(message =>
            {
                switch (message.Text)
                {
                    case "to-respond":
                        ws.Send("response");
                        return;
                    case "to-block":
                        return;
                    case "to-modify":
                        server.Send("modified");
                        return;
                }
                server.Send(message.Text);
            });
            server.OnMessage(message =>
            {
                switch (message.Text)
                {
                    case "to-block":
                        return;
                    case "to-modify":
                        ws.Send("modified");
                        return;
                }
                ws.Send(message.Text);
            });
            server.Send("fake");
            tcs.SetResult(ws);
        });

        var wsTask = Server.WaitForWebSocketAsync();
        var log = new List<string>();
        Server.OnceWebSocketConnection((ws, _) =>
        {
            ws.MessageReceived += (_, message) => log.Add($"message: {message}");
            ws.Closed += (_, ev) => log.Add($"close: code={ev.Code} reason={ev.Reason}");
            return Task.CompletedTask;
        });

        await SetupWS(Page, Server.Port, "blob");
        var ws = await wsTask;
        await AssertAreEqualWithRetriesAsync(() => Task.FromResult(log.ToArray()), new[] { "message: fake" });

        await ws.SendAsync("to-modify");
        await ws.SendAsync("to-block");
        await ws.SendAsync("pass-server");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[]
        {
            "open",
            $"message: data=modified origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=pass-server origin=ws://localhost:{Server.Port} lastEventId=",
        });

        await Page.EvaluateAsync(@"() => {
            window.ws.send('to-respond');
            window.ws.send('to-modify');
            window.ws.send('to-block');
            window.ws.send('pass-client');
        }");
        await AssertAreEqualWithRetriesAsync(() => Task.FromResult(log.ToArray()), new[] { "message: fake", "message: modified", "message: pass-client" });
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[]
        {
            "open",
            $"message: data=modified origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=pass-server origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=response origin=ws://localhost:{Server.Port} lastEventId=",
        });

        var route = await tcs.Task;
        route.Send("another");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[]
        {
            "open",
            $"message: data=modified origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=pass-server origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=response origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=another origin=ws://localhost:{Server.Port} lastEventId=",
        });

        await Page.EvaluateAsync("() => { window.ws.send('pass-client-2'); }");
        await AssertAreEqualWithRetriesAsync(() => Task.FromResult(log.ToArray()), new[] { "message: fake", "message: modified", "message: pass-client", "message: pass-client-2" });

        await Page.EvaluateAsync("() => { window.ws.close(3009, 'problem'); }");
        await AssertAreEqualWithRetriesAsync(() => Task.FromResult(log.ToArray()), new[] { "message: fake", "message: modified", "message: pass-client", "message: pass-client-2", "close: code=3009 reason=problem" });
    }

    [PlaywrightTest("page-route-web-socket.spec.ts", "should work without server")]
    public async Task ShouldWorkWithoutServer()
    {
        var tcs = new TaskCompletionSource<IWebSocketRoute>();
        await Page.RouteWebSocketAsync(new Regex(".*"), ws =>
        {
            ws.OnMessage(message =>
            {
                if (message.Text == "to-respond")
                {
                    ws.Send("response");
                }
            });
            tcs.SetResult(ws);
        });

        await SetupWS(Page, Server.Port, "blob");

        await Page.EvaluateAsync(@"async () => {
            await window.wsOpened;
            window.ws.send('to-respond');
            window.ws.send('to-block');
            window.ws.send('to-respond');
        }");

        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[]
        {
            "open",
            $"message: data=response origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=response origin=ws://localhost:{Server.Port} lastEventId=",
        });

        var route = await tcs.Task;
        route.Send("another");
        await route.CloseAsync(new() { Code = 3008, Reason = "oops" });

        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"),
        [
            "open",
            $"message: data=response origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=response origin=ws://localhost:{Server.Port} lastEventId=",
            $"message: data=another origin=ws://localhost:{Server.Port} lastEventId=",
            "close code=3008 reason=oops wasClean=true",
        ]);
    }

    [PlaywrightTest("page-route-web-socket.spec.ts", "should work with baseURL")]
    public async Task ShouldWorkWithBaseURL()
    {
        var context = await Browser.NewContextAsync(new() { BaseURL = $"http://localhost:{Server.Port}" });
        var page = await context.NewPageAsync();

        await page.RouteWebSocketAsync("/ws", ws =>
        {
            ws.OnMessage(message =>
            {
                ws.Send(message.Text);
            });
        });

        await SetupWS(page, Server.Port, "blob");

        await page.EvaluateAsync(@"async () => {
            await window.wsOpened;
            window.ws.send('echo');
        }");
        await AssertAreEqualWithRetriesAsync(() => page.EvaluateAsync<string[]>("() => window.log"), new[]
        {
            "open",
            $"message: data=echo origin=ws://localhost:{Server.Port} lastEventId=",
        });
    }

    [PlaywrightTest("page-route-web-socket.spec.ts", "should work with no trailing slash")]
    public async Task ShouldWorkWithNoTrailingSlash()
    {
        var log = new List<string>();
        // No trailing slash!
        await Page.RouteWebSocketAsync($"ws://localhost:{Server.Port}", ws =>
        {
            ws.OnMessage(message =>
            {
                log.Add(message.Text);
                ws.Send("response");
            });
        });

        await Page.GotoAsync("about:blank");
        await Page.EvaluateAsync(@"({ port }) => {
            window.log = [];
            // No trailing slash in WebSocket URL
            window.ws = new WebSocket('ws://localhost:' + port);
            window.ws.addEventListener('message', event => window.log.push(event.data));
        }", new Dictionary<string, object>
        {
            ["port"] = Server.Port
        });

        await Page.WaitForFunctionAsync("() => window.ws.readyState === 1");
        await Page.EvaluateAsync("() => window.ws.send('query')");
        await AssertAreEqualWithRetriesAsync(
            () => Task.FromResult(log.ToArray()),
            ["query"]);
        await AssertAreEqualWithRetriesAsync(
            () => Page.EvaluateAsync<string[]>("() => window.log"),
            ["response"]);
    }
}
