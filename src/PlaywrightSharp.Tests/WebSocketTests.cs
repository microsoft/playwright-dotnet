using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>web-socket.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WebSocketTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WebSocketTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("web-socket.spec.ts", "should work")]
        [Fact]
        public async Task ShouldWork()
        {
            string value = await Page.EvaluateAsync<string>(@"port => {
                let cb;
                const result = new Promise(f => cb = f);
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('message', data => { ws.close(); cb(data.data); });
                return result;
            }", TestConstants.Port);
            Assert.Equal("incoming", value);
        }

        [PlaywrightTest("web-socket.spec.ts", "should emit close events")]
        [Fact]
        public async Task ShouldEmitCloseEvents()
        {
            var socketClosedTcs = new TaskCompletionSource<bool>();
            var log = new List<string>();
            IWebSocket webSocket = null;

            Page.WebSocket += (_, e) =>
            {
                log.Add($"open<{e.WebSocket.Url}>");
                webSocket = e.WebSocket;
                webSocket.Close += (_, __) =>
                {
                    log.Add("close");
                    socketClosedTcs.TrySetResult(true);
                };
            };

            await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('open', () => ws.close());
            }", TestConstants.Port);

            await socketClosedTcs.Task.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.Equal($"open<ws://localhost:{TestConstants.Port}/ws>:close", string.Join(":", log));
            Assert.True(webSocket.IsClosed);
        }

        [PlaywrightTest("web-socket.spec.ts", "should emit frame events")]
        [Fact]
        public async Task ShouldEmitFrameEvents()
        {
            var socketClosedTcs = new TaskCompletionSource<bool>();
            var log = new List<string>();

            Page.WebSocket += (_, e) =>
            {
                log.Add($"open");

                e.WebSocket.FrameSent += (_, e) => log.Add($"sent<{e.Payload}>");
                e.WebSocket.FrameReceived += (_, e) => log.Add($"received<{e.Payload}>");

                e.WebSocket.Close += (_, __) =>
                {
                    log.Add("close");
                    socketClosedTcs.TrySetResult(true);
                };
            };

            await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://127.0.0.1:' + port + '/ws');
                ws.addEventListener('open', () => { ws.send('outgoing'); });
                ws.addEventListener('message', e => { ws.close() });
            }", TestConstants.Port);

            await socketClosedTcs.Task.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.Equal("open", log[0]);
            Assert.Equal("close", log[3]);
            log.Sort();
            Assert.Equal("close:open:received<incoming>:sent<outgoing>", string.Join(":", log));
        }

        [PlaywrightTest("web-socket.spec.ts", "should emit binary frame events")]
        [Fact]
        public async Task ShouldEmitBinaryFrameEvents()
        {
            var socketClosedTcs = new TaskCompletionSource<bool>();
            var log = new List<WebSocketFrameEventArgs>();

            Page.WebSocket += (_, e) =>
            {
                e.WebSocket.FrameSent += (_, e) => log.Add(e);
                e.WebSocket.Close += (_, __) => socketClosedTcs.TrySetResult(true);
            };

            await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('open', () => {
                    const binary = new Uint8Array(5);
                    for (let i = 0; i < 5; ++i)
                        binary[i] = i;
                    ws.send('text');
                    ws.send(binary);
                    ws.close();
                });
            }", TestConstants.Port);


            await socketClosedTcs.Task.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.Equal("text", log[0].Payload);

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i, log[1].Payload.AsBinary()[i]);
            }
        }

        [PlaywrightTest("web-socket.spec.ts", "should emit error")]
        [Fact]
        public async Task ShouldEmitError()
        {
            var socketErrorTcs = new TaskCompletionSource<string>();

            Page.WebSocket += (_, e) =>
            {
                e.WebSocket.SocketError += (_, e) => socketErrorTcs.TrySetResult(e.ErrorMessage);
            };

            await Page.EvaluateAsync(@"port => {
                new WebSocket('ws://localhost:' + port + '/bogus-ws');
            }", TestConstants.Port);


            await socketErrorTcs.Task.WithTimeout(TestConstants.DefaultTaskTimeout);

            if (TestConstants.IsFirefox)
            {
                Assert.Equal("CLOSE_ABNORMAL", socketErrorTcs.Task.Result);
            }
            else
            {
                Assert.Contains(": 40", socketErrorTcs.Task.Result);
            }
        }

        [PlaywrightTest("web-socket.spec.ts", "should not have stray error events")]
        [Fact]
        public async Task ShouldNotHaveStrayErrorEvents()
        {
            var frameReceivedTcs = new TaskCompletionSource<bool>();
            WebSocketErrorEventArgs socketError = null;
            IWebSocket ws = null;

            Page.WebSocket += (_, e) =>
            {
                ws = e.WebSocket;
                e.WebSocket.SocketError += (_, e) => socketError = e;
                e.WebSocket.FrameReceived += (_, e) => frameReceivedTcs.TrySetResult(true);
            };

            await TaskUtils.WhenAll(
                frameReceivedTcs.Task,
                Page.EvaluateAsync(@"port => {
                    window.ws = new WebSocket('ws://localhost:' + port + '/ws');
                }", TestConstants.Port));

            await Page.EvaluateAsync("window.ws.close();");
            Assert.Null(socketError);
        }

        [PlaywrightTest("web-socket.spec.ts", "should reject waitForEvent on socket close")]
        [Fact]
        public async Task ShouldRejectWaitForEventOnSocketClose()
        {
            var frameReceivedTcs = new TaskCompletionSource<bool>();
            IWebSocket ws = null;

            Page.WebSocket += (_, e) =>
            {
                ws = e.WebSocket;
                e.WebSocket.FrameReceived += (_, e) => frameReceivedTcs.TrySetResult(true);
            };

            await TaskUtils.WhenAll(
                frameReceivedTcs.Task,
                Page.EvaluateAsync(@"port => {
                    window.ws = new WebSocket('ws://localhost:' + port + '/ws');
                }", TestConstants.Port));

            await frameReceivedTcs.Task;
            var frameSentTask = ws.WaitForEventAsync(WebSocketEvent.FrameSent);
            await Page.EvaluateAsync("window.ws.close()");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => frameSentTask);
            Assert.Contains("Socket closed", exception.Message);
        }

        [PlaywrightTest("web-socket.spec.ts", "should reject waitForEvent on page close")]
        [Fact]
        public async Task ShouldRejectWaitForEventOnPageClose()
        {
            var frameReceivedTcs = new TaskCompletionSource<bool>();
            IWebSocket ws = null;

            Page.WebSocket += (_, e) =>
            {
                ws = e.WebSocket;
                e.WebSocket.FrameReceived += (_, e) => frameReceivedTcs.TrySetResult(true);
            };

            await TaskUtils.WhenAll(
                frameReceivedTcs.Task,
                Page.EvaluateAsync(@"port => {
                    window.ws = new WebSocket('ws://localhost:' + port + '/ws');
                }", TestConstants.Port));

            var frameSentTask = ws.WaitForEventAsync(WebSocketEvent.FrameSent);
            await Page.CloseAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => frameSentTask.WithTimeout(TestConstants.DefaultTaskTimeout));
            Assert.Contains("Page closed", exception.Message);
        }
    }
}
