using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private const int DefaultBufferSize = 1024;  // Byte buffer size
        private readonly ClientWebSocket _webSocket;
        private readonly string _wsEndpoint;
        private readonly CancellationTokenSource _readerCancellationSource = new();
        private readonly BrowserTypeConnectOptions _options;

        internal WebSocketTransport(
            string wsEndpoint = default,
            BrowserTypeConnectOptions options = default)
        {
            _webSocket = new ClientWebSocket();
            _wsEndpoint = wsEndpoint;
            _options = options;
            SetOptions();
            _ = ConnectAsync();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~WebSocketTransport() => Dispose(false);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<LogReceivedEventArgs> LogReceived;

        public event EventHandler<TransportClosedEventArgs> TransportClosed;

        public bool IsClosed { get; private set; }

        public async Task SendAsync(string message)
        {
            var slowMo = _options.SlowMo;
            if (slowMo != null && slowMo > 0)
            {
                await Task.Delay((int)slowMo).ConfigureAwait(false);
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / DefaultBufferSize);

            try
            {
                if (!_readerCancellationSource.IsCancellationRequested)
                {
                    for (var i = 0; i < messagesCount; i++)
                    {
                        var offset = DefaultBufferSize * i;
                        var count = DefaultBufferSize;
                        var lastMessage = (i + 1) == messagesCount;

                        if ((count * (i + 1)) > messageBuffer.Length)
                        {
                            count = messageBuffer.Length - offset;
                        }

                        await _webSocket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _readerCancellationSource.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        /// <inheritdoc/>
        public void Close(string closeReason)
        {
            if (!IsClosed)
            {
                IsClosed = true;
                _ = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeReason, _readerCancellationSource.Token).ConfigureAwait(false);
                TransportClosed?.Invoke(this, new TransportClosedEventArgs { CloseReason = closeReason });
                _readerCancellationSource?.Cancel();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Close(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Close(ex.ToString());
        }

        private async Task ConnectAsync()
        {
            await _webSocket.ConnectAsync(new Uri(_wsEndpoint), _readerCancellationSource.Token).ConfigureAwait(false);
            ScheduleTransportTask(ReceiveAsync, _readerCancellationSource.Token);
        }

        private void ScheduleTransportTask(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(() => func(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        private async Task ReceiveAsync(CancellationToken token)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Closed)
                {
                    Close("Closed");
                }
                else
                {
                    var buffer = WebSocket.CreateClientBuffer(DefaultBufferSize, DefaultBufferSize);

                    while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result;
                        using (MemoryStream memoryStream = new())
                        {
                            do
                            {
                                result = await _webSocket.ReceiveAsync(buffer, token).ConfigureAwait(false);

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await
                                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _readerCancellationSource.Token).ConfigureAwait(false);
                                }
                                else
                                {
#if NETSTANDARD
                                    await memoryStream.WriteAsync(buffer.Array, 0, result.Count, _readerCancellationSource.Token).ConfigureAwait(false);
#else
                                    await memoryStream.WriteAsync(new(buffer.Array, 0, result.Count), _readerCancellationSource.Token).ConfigureAwait(false);
#endif
                                }
                            }
                            while (!result.EndOfMessage);

                            string output = Encoding.UTF8.GetString(memoryStream.ToArray());
                            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(output));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _readerCancellationSource?.Dispose();
            _webSocket?.Dispose();
        }

        private string GenerateUserAgent()
        {
            var architecture = RuntimeInformation.OSArchitecture;
            var osAndVersion = RuntimeInformation.OSDescription;
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            var assemblyVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            return $"Playwright/{assemblyVersion} {frameworkDescription} ({architecture}/{osAndVersion})";
        }

        private void SetOptions()
        {
            _webSocket.Options.SetRequestHeader("User-Agent", GenerateUserAgent());
            if (_options.Headers is IDictionary<string, string> dictionary && dictionary.Keys.Any(f => f != null))
            {
                foreach (var kv in dictionary)
                {
                    if (kv.Value != null)
                    {
                        _webSocket.Options.SetRequestHeader(kv.Key, kv.Value);
                    }
                }
            }

            var timeout = _options.Timeout;
            if (timeout != null)
            {
                _webSocket.Options.KeepAliveInterval = TimeSpan.FromMilliseconds((double)timeout);
            }
        }
    }
}
