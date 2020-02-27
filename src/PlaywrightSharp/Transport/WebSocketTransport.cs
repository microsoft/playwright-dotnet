using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport
{
    internal class WebSocketTransport : IConnectionTransport, IDisposable
    {
        /// <summary>
        /// Gets the default <see cref="TransportTaskScheduler"/>.
        /// </summary>
        public static readonly TransportTaskScheduler DefaultTransportScheduler = ScheduleTransportTask;
        private readonly WebSocket _webSocket;
        private readonly bool _enqueueTransportMessages;
        private readonly TaskQueue _socketQueue = new TaskQueue();
        private CancellationTokenSource _readerCancellationSource = new CancellationTokenSource();

        public WebSocketTransport(WebSocket webSocket, TransportTaskScheduler scheduler, bool enqueueTransportMessages)
        {
            _webSocket = webSocket;
            _enqueueTransportMessages = enqueueTransportMessages;

            scheduler(GetResponseAsync, _readerCancellationSource.Token);
        }

        public event EventHandler<TransportClosedEventArgs> Closed;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        internal bool IsClosed { get; private set; }

        public void Close(string closeReason = null)
        {
            if (!IsClosed)
            {
                IsClosed = true;
                StopReading();
                Closed?.Invoke(this, new TransportClosedEventArgs(closeReason));
            }
        }

        public Task SendAsync(string message, object arguments)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            Task SendCoreAsync() => _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, default);

            return _enqueueTransportMessages ? _socketQueue.Enqueue(SendCoreAsync) : SendCoreAsync();
        }

        public void Dispose()
        {
            // Make sure any outstanding asynchronous read operation is cancelled.
            StopReading();
            _webSocket?.Dispose();
            _socketQueue?.Dispose();
            _readerCancellationSource?.Dispose();
        }

        internal static Task<IConnectionTransport> CreateAsync(ConnectOptions options)
            => CreateAsync(options.BrowserWSEndpoint, options.EnqueueTransportMessages);

        internal static async Task<IConnectionTransport> CreateAsync(string browserWSEndpoint, bool enqueueTransportMessages)
        {
            var webSocket = await CreateWebSocket(browserWSEndpoint).ConfigureAwait(false);
            return new WebSocketTransport(webSocket, DefaultTransportScheduler, enqueueTransportMessages);
        }

        private static async Task<WebSocket> CreateWebSocket(string url)
        {
            var result = new ClientWebSocket();
            result.Options.KeepAliveInterval = TimeSpan.Zero;
            await result.ConnectAsync(new Uri(url), CancellationToken.None).ConfigureAwait(false);
            return result;
        }

        private static void ScheduleTransportTask(Func<CancellationToken, Task> taskFactory, CancellationToken cancellationToken)
            => Task.Factory.StartNew(
                () => taskFactory(cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        private async Task<object> GetResponseAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[2048];

            while (!IsClosed)
            {
                bool endOfMessage = false;
                var response = new StringBuilder();

                while (!endOfMessage)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await _webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }

                    // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types.
                    {
                        OnClose(ex.Message);
                        return null;
                    }

                    endOfMessage = result.EndOfMessage;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        response.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        OnClose("WebSocket closed");
                        return null;
                    }
                }

                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(response.ToString()));
            }

            return null;
        }

        private void OnClose(string closeReason)
        {
            if (!IsClosed)
            {
                IsClosed = true;
                StopReading();
                Closed?.Invoke(this, new TransportClosedEventArgs(closeReason));
            }
        }

        private void StopReading()
        {
            var readerCts = Interlocked.CompareExchange(ref _readerCancellationSource, null, _readerCancellationSource);
            if (readerCts != null)
            {
                // Asynchronous read operations may still be in progress, so cancel it first and then dispose
                // the associated CancellationTokenSource.
                readerCts.Cancel();
                readerCts.Dispose();
            }
        }
    }
}
