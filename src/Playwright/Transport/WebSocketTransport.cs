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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport
{
    internal class WebSocketTransport : IConnectionTransport, IDisposable
    {
        private const int DefaultBufferSize = 16000;  // Byte buffer size
        private readonly ClientWebSocket _webSocket;
        private readonly string _wsEndpoint;
        private readonly BrowserTypeConnectOptions _options;
        private readonly float _slowMo;
        private CancellationTokenSource _webSocketToken;

        internal WebSocketTransport(
            string wsEndpoint = default,
            BrowserTypeConnectOptions options = default)
        {
            _webSocket = new ClientWebSocket();
            _webSocketToken = new CancellationTokenSource();
            _wsEndpoint = wsEndpoint;
            _options = options;
            _slowMo = _options?.SlowMo ?? 0;
            SetRequestHeaders();
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<LogReceivedEventArgs> LogReceived;

        public event EventHandler<TransportClosedEventArgs> TransportClosed;

        private bool IsClosed { get; set; }

        public async Task SendAsync(string message)
        {
            if (_slowMo > 0)
                await Task.Delay((int)_slowMo).ConfigureAwait(false);

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBuffer, 0, messageBuffer.Length), WebSocketMessageType.Text, true, _webSocketToken.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // ReceiveAsync won't throw, so we need to handle it here.
                HandleSocketClosed(e.ToString());
            }
        }

        public async Task ConnectAsync()
        {
            var timeout = _options?.Timeout ?? 30000;
            using var connectCancellationSource = new CancellationTokenSource((int)timeout);
            await _webSocket.ConnectAsync(new Uri(_wsEndpoint), connectCancellationSource.Token).ConfigureAwait(false);
            _ = Task.Factory.StartNew(() => DispatchIncomingMessagesAsync(), _webSocketToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private async Task DispatchIncomingMessagesAsync()
        {
#pragma warning disable VSTHRD103
            var buffer = WebSocket.CreateClientBuffer(DefaultBufferSize, DefaultBufferSize);
            var memoryStream = new MemoryStream();
            string closeReason = string.Empty;
            try
            {
                while (true)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(buffer, _webSocketToken.Token).ConfigureAwait(false);

                    if (_webSocket.State != WebSocketState.Open)
                        break;

                    memoryStream.Write(buffer.Array, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        string output = Encoding.UTF8.GetString(memoryStream.ToArray());
                        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(output));
                        memoryStream.Dispose();
                        memoryStream = new MemoryStream();
                    }
                }
            }
            catch (Exception ex)
            {
                closeReason = ex.ToString();
            }

            // Does not matter whether this is error or not, report
            // transport as closed, close web socket if open.
            HandleSocketClosed(closeReason);
            memoryStream.Dispose();
#pragma warning restore VSTHRD103
        }

        public void Close(string closeReason)
        {
            _webSocketToken.Cancel();
            HandleSocketClosed(closeReason);
        }

        private void HandleSocketClosed(string closeReason)
        {
            if (IsClosed)
                return;
            IsClosed = true;
            TransportClosed?.Invoke(this, new TransportClosedEventArgs { CloseReason = closeReason });
            if (_webSocket.State == WebSocketState.Open)
                _ = _webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._webSocketToken.Dispose();
                this._webSocket.Dispose();
            }
        }

        private void SetRequestHeaders()
        {
            var architecture = RuntimeInformation.OSArchitecture;
            var osAndVersion = RuntimeInformation.OSDescription;
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            var assemblyVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            var userAgent = $"Playwright/{assemblyVersion} {frameworkDescription} ({architecture}/{osAndVersion})";
            _webSocket.Options.SetRequestHeader("User-Agent", userAgent);
            foreach (var item in _options?.Headers ?? Array.Empty<KeyValuePair<string, string>>())
            {
                _webSocket.Options.SetRequestHeader(item.Key, item.Value);
            }
        }
    }
}
