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

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

///<playwright-file>capabilities.spec.ts</playwright-file>
public class CapabilitiesTests : PageTestEx
{
    [PlaywrightTest("capabilities.spec.ts", "Web Assembly should work")]
    [Skip(SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Windows)]
    public async Task WebAssemblyShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/wasm/table2.html");
        Assert.AreEqual("42, 83", await Page.EvaluateAsync<string>("() => loadTable()"));
    }

    [PlaywrightTest("capabilities.spec.ts", "WebSocket should work")]
    [Skip(SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Windows)]
    public async Task WebSocketShouldWork()
    {
        Server.SendOnWebSocketConnection("incoming");
        string value = await Page.EvaluateAsync<string>(
            $@"(port) => {{
                    let cb;
                    const result = new Promise(f => cb = f);
                    const ws = new WebSocket('ws://localhost:' + port + '/ws');
                    ws.addEventListener('message', data => {{ ws.close(); cb(data.data); console.log(data); console.log(data.data) }});
                    ws.addEventListener('error', error => cb('Error'));
                    return result;
                }}",
            Server.Port);
        Assert.AreEqual("incoming", value);
    }

    [PlaywrightTest("capabilities.spec.ts", "should respect CSP")]
    public async Task ShouldRespectCSP()
    {
        Server.SetRoute("/empty.html", context =>
        {
            const string message = @"
                    <script>
                      window.testStatus = 'SUCCESS';
                      window.testStatus = eval(""'FAILED'"");
                    </script>
                ";

            context.Response.Headers["Content-Length"] = message.Length.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["Content-Security-Policy"] = "script-src 'unsafe-inline';";
            return context.Response.WriteAsync(message);
        });

        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("SUCCESS", await Page.EvaluateAsync<string>("() => window.testStatus"));
    }

    [PlaywrightTest("capabilities.spec.ts", "should play video")]
    [Skip(SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Linux, SkipAttribute.Targets.Webkit | SkipAttribute.Targets.Windows, SkipAttribute.Targets.Firefox)]
    public async Task ShouldPlayVideo()
    {
        await Page.GotoAsync(Server.Prefix + (TestConstants.IsWebKit ? "/video_mp4.html" : "/video.html"));
        await Page.EvalOnSelectorAsync("video", "v => v.play()");
        await Page.EvalOnSelectorAsync("video", "v => v.pause()");
    }
}
