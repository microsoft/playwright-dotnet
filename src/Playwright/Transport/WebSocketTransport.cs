using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Playwright.Transport
{
    internal class WebSocketTransport : IConnectionTransport, IDisposable
    {
        private readonly CancellationTokenSource _readerCancellationSource = new();
        private readonly ClientWebSocket _webSocket;
        private readonly ILogger<WebSocketTransport> _logger;
        private readonly List<byte> _data = new();
        private int? _currentMessageSize;

        internal WebSocketTransport(
            Uri uri = default,
            IEnumerable<KeyValuePair<string, string>> headers = default,
            float? timeout = default,
            float? slowMo = default,
            ILoggerFactory loggerFactory = default)
        {
            _logger = loggerFactory?.CreateLogger<WebSocketTransport>();
            _webSocket = new ClientWebSocket();
            _readerCancellationSource.CancelAfter((int)timeout);
            _webSocket.ConnectAsync(uri, _readerCancellationSource.Token).ConfigureAwait(false);
            ScheduleTransportTask(ReceiveAsync, _readerCancellationSource.Token);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~WebSocketTransport() => Dispose(false);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<LogReceivedEventArgs> LogReceived;

        public event EventHandler<TransportClosedEventArgs> TransportClosed;

        public bool IsClosed { get; private set; }

        public async Task SendAsync(string message)
        {
            try
            {
                if (!_readerCancellationSource.IsCancellationRequested)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message.ToCharArray());
                    int len = bytes.Length;
                    byte[] ll = new byte[4];
                    ll[0] = (byte)(len & 0xFF);
                    ll[1] = (byte)((len >> 8) & 0xFF);
                    ll[2] = (byte)((len >> 16) & 0xFF);
                    ll[3] = (byte)((len >> 24) & 0xFF);

                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, false, _readerCancellationSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Transport Error");
                Close(ex.ToString());
            }
        }

        /// <inheritdoc/>
        public void Close(string closeReason)
        {
            if (!IsClosed)
            {
                IsClosed = true;
                TransportClosed?.Invoke(this, new TransportClosedEventArgs { CloseReason = closeReason });
                _readerCancellationSource.Cancel();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ScheduleTransportTask(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(() => func(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        private async Task ReceiveAsync(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _readerCancellationSource.Token).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, _readerCancellationSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        _data.AddRange(buffer.AsMemory().Slice(0, result.Count).ToArray());

                        ProcessStream(result.EndOfMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Transport Error");
                Close(ex.ToString());
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_readerCancellationSource != null)
            {
                _readerCancellationSource.Cancel();
                _readerCancellationSource.Dispose();
            }

            if (_webSocket != null)
            {
                _webSocket.Dispose();
            }
        }

        private void ProcessStream(bool isEndOfMessage)
        {
            while (!isEndOfMessage)
            {
                if (_currentMessageSize == null && _data.Count < 4)
                {
                    break;
                }

                if (_currentMessageSize == null)
                {
                    _currentMessageSize = _data[0] + (_data[1] << 8) + (_data[2] << 16) + (_data[3] << 24);
                    _data.RemoveRange(0, 4);
                }

                if (_data.Count < _currentMessageSize)
                {
                    break;
                }

                string result = System.Text.Encoding.UTF8.GetString(_data.GetRange(0, _currentMessageSize.Value).ToArray());
                _data.RemoveRange(0, _currentMessageSize.Value);
                _currentMessageSize = null;
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(result));
            }
        }
    }
}
