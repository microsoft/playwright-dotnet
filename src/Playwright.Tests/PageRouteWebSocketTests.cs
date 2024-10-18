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

// test('should work with ws.close', async ({ page, server }) => {
//   const { promise, resolve } = withResolvers<WebSocketRoute>();
//   await page.routeWebSocket(/.*/, async ws => {
//     ws.connectToServer();
//     resolve(ws);
//   });

//   const wsPromise = server.waitForWebSocket();
//   await setupWS(page, server.PORT, 'blob');
//   const ws = await wsPromise;

//   const route = await promise;
//   route.send('hello');
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=hello origin=ws://localhost:${server.PORT} lastEventId=`,
//   ]);

//   const closedPromise = new Promise(f => ws.once('close', (code, reason) => f({ code, reason: reason.toString() })));
//   await route.close({ code: 3009, reason: 'oops' });
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=hello origin=ws://localhost:${server.PORT} lastEventId=`,
//     'close code=3009 reason=oops wasClean=true',
//   ]);
//   expect(await closedPromise).toEqual({ code: 3009, reason: 'oops' });
// });

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
            Console.WriteLine($"Result: {string.Join(", ", result)}");
            Console.WriteLine($"Expected: {string.Join(", ", expected)}");
            if (result.SequenceEqual(expected))
            {
                return;
            }
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
        await Page.PauseAsync();
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
        ws.MessageReceived += (message, _) => {
            Console.WriteLine("Received message: " + message);
            ws.SendAsync("response").GetAwaiter().GetResult();
        };

        await Page.EvaluateAsync("() => window.ws1.send('request')");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[] { "ws1:response" });

        await Page.EvaluateAsync("() => window.ws2.send('request')");
        await AssertAreEqualWithRetriesAsync(() => Page.EvaluateAsync<string[]>("() => window.log"), new[] { "ws1:response", "ws2:mock-response" });
    }


// test('should pattern match', async ({ page, server }) => {
//   await page.routeWebSocket(/.*\/ws$/, async ws => {
//     ws.connectToServer();
//   });

//   await page.routeWebSocket('**/mock-ws', ws => {
//     ws.onMessage(message => {
//       ws.send('mock-response');
//     });
//   });

//   const wsPromise = server.waitForWebSocket();

//   await page.goto('about:blank');
//   await page.evaluate(async ({ port }) => {
//     window.log = [];
//     (window as any).ws1 = new WebSocket('ws://localhost:' + port + '/ws');
//     (window as any).ws1.addEventListener('message', event => window.log.push(`ws1:${event.data}`));
//     (window as any).ws2 = new WebSocket('ws://localhost:' + port + '/something/something/mock-ws');
//     (window as any).ws2.addEventListener('message', event => window.log.push(`ws2:${event.data}`));
//     await Promise.all([
//       new Promise(f => (window as any).ws1.addEventListener('open', f)),
//       new Promise(f => (window as any).ws2.addEventListener('open', f)),
//     ]);
//   }, { port: server.PORT });

//   const ws = await wsPromise;
//   ws.on('message', () => ws.send('response'));

//   await page.evaluate(() => (window as any).ws1.send('request'));
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([`ws1:response`]);

//   await page.evaluate(() => (window as any).ws2.send('request'));
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([`ws1:response`, `ws2:mock-response`]);
// });

// test('should work with server', async ({ page, server }) => {
//   const { promise, resolve } = withResolvers<WebSocketRoute>();
//   await page.routeWebSocket(/.*/, async ws => {
//     const server = ws.connectToServer();
//     ws.onMessage(message => {
//       switch (message) {
//         case 'to-respond':
//           ws.send('response');
//           return;
//         case 'to-block':
//           return;
//         case 'to-modify':
//           server.send('modified');
//           return;
//       }
//       server.send(message);
//     });
//     server.onMessage(message => {
//       switch (message) {
//         case 'to-block':
//           return;
//         case 'to-modify':
//           ws.send('modified');
//           return;
//       }
//       ws.send(message);
//     });
//     server.send('fake');
//     resolve(ws);
//   });

//   const wsPromise = server.waitForWebSocket();
//   const log: string[] = [];
//   server.onceWebSocketConnection(ws => {
//     ws.on('message', data => log.push(`message: ${data.toString()}`));
//     ws.on('close', (code, reason) => log.push(`close: code=${code} reason=${reason.toString()}`));
//   });

//   await setupWS(page, server.PORT, 'blob');
//   const ws = await wsPromise;
//   await expect.poll(() => log).toEqual(['message: fake']);

//   ws.send('to-modify');
//   ws.send('to-block');
//   ws.send('pass-server');
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=modified origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=pass-server origin=ws://localhost:${server.PORT} lastEventId=`,
//   ]);

//   await page.evaluate(() => {
//     window.ws.send('to-respond');
//     window.ws.send('to-modify');
//     window.ws.send('to-block');
//     window.ws.send('pass-client');
//   });
//   await expect.poll(() => log).toEqual(['message: fake', 'message: modified', 'message: pass-client']);
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=modified origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=pass-server origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=response origin=ws://localhost:${server.PORT} lastEventId=`,
//   ]);

//   const route = await promise;
//   route.send('another');
//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=modified origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=pass-server origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=response origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=another origin=ws://localhost:${server.PORT} lastEventId=`,
//   ]);

//   await page.evaluate(() => {
//     window.ws.send('pass-client-2');
//   });
//   await expect.poll(() => log).toEqual(['message: fake', 'message: modified', 'message: pass-client', 'message: pass-client-2']);

//   await page.evaluate(() => {
//     window.ws.close(3009, 'problem');
//   });
//   await expect.poll(() => log).toEqual(['message: fake', 'message: modified', 'message: pass-client', 'message: pass-client-2', 'close: code=3009 reason=problem']);
// });

// test('should work without server', async ({ page, server }) => {
//   const { promise, resolve } = withResolvers<WebSocketRoute>();
//   await page.routeWebSocket(/.*/, ws => {
//     ws.onMessage(message => {
//       switch (message) {
//         case 'to-respond':
//           ws.send('response');
//           return;
//       }
//     });
//     resolve(ws);
//   });

//   await setupWS(page, server.PORT, 'blob');

//   await page.evaluate(async () => {
//     await window.wsOpened;
//     window.ws.send('to-respond');
//     window.ws.send('to-block');
//     window.ws.send('to-respond');
//   });

//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=response origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=response origin=ws://localhost:${server.PORT} lastEventId=`,
//   ]);

//   const route = await promise;
//   route.send('another');
//   await route.close({ code: 3008, reason: 'oops' });

//   await expect.poll(() => page.evaluate(() => window.log)).toEqual([
//     'open',
//     `message: data=response origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=response origin=ws://localhost:${server.PORT} lastEventId=`,
//     `message: data=another origin=ws://localhost:${server.PORT} lastEventId=`,
//     'close code=3008 reason=oops wasClean=true',
//   ]);
// });
}