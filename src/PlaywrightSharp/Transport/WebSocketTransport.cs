using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class WebSocketTransport : IConnectionTransport
    {
        /// <summary>
        /// Gets the default <see cref="TransportTaskScheduler"/>.
        /// </summary>
        public static readonly TransportTaskScheduler DefaultTransportScheduler = ScheduleTransportTask;
        private readonly WebSocket _webSocket;
        private readonly TransportTaskScheduler _defaultTransportScheduler;
        private readonly bool _enqueueTransportMessages;

        public WebSocketTransport()
        {
        }

        public WebSocketTransport(WebSocket webSocket, TransportTaskScheduler defaultTransportScheduler, bool enqueueTransportMessages)
        {
            _webSocket = webSocket;
            _defaultTransportScheduler = defaultTransportScheduler;
            _enqueueTransportMessages = enqueueTransportMessages;
        }

        public event EventHandler<TransportClosedEventArgs> Closed;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public void Close(string closeReason = null)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(string message, object arguments)
        {
            throw new NotImplementedException();
        }

        internal static async Task<IConnectionTransport> CreateAsync(ConnectOptions options)
        {
            var webSocket = await CreateWebSocket(options.BrowserWSEndpoint).ConfigureAwait(false);
            return new WebSocketTransport(webSocket, DefaultTransportScheduler, options.EnqueueTransportMessages);
        }

        private static void ScheduleTransportTask(Func<CancellationToken, Task> taskFactory, CancellationToken cancellationToken)
            => Task.Factory.StartNew(
                () => taskFactory(cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        private static async Task<WebSocket> CreateWebSocket(string url)
        {
            var result = new ClientWebSocket();
            result.Options.KeepAliveInterval = TimeSpan.Zero;
            await result.ConnectAsync(new Uri(url), CancellationToken.None).ConfigureAwait(false);
            return result;
        }
    }
}
