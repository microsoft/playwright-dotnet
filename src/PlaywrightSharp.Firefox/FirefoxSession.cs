using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Messaging;
using PlaywrightSharp.Firefox.Protocol;
using PlaywrightSharp.Messaging;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxSession
    {
        private readonly string _targetType;
        private readonly string _sessionId;
        private readonly Func<int, IFirefoxRequest<IFirefoxResponse>, Task> _rawSend;
        private readonly string _closeReason = string.Empty;
        private readonly ConcurrentDictionary<int, MessageTask<IFirefoxResponse>> _callbacks = new ConcurrentDictionary<int, MessageTask<IFirefoxResponse>>();
        private bool _disposed = false;

        public FirefoxSession(FirefoxConnection connection, string targetType, string sessionId, Func<int, IFirefoxRequest<IFirefoxResponse>, Task> rawSend)
        {
            Connection = connection;
            _targetType = targetType;
            _sessionId = sessionId;
            _rawSend = rawSend;

            Connection.MessageReceived += OnMessageReceived;
            connection.Disconnected += OnDisconnected;
        }

        public event EventHandler<IFirefoxEvent> MessageReceived;

        public event EventHandler<EventArgs> Disconnected;

        public bool IsClosed { get; internal set; }

        internal FirefoxConnection Connection { get; }

        internal async Task<TFirefoxResponse> SendAsync<TFirefoxResponse>(IFirefoxRequest<TFirefoxResponse> request, bool waitForCallback = true)
            where TFirefoxResponse : IFirefoxResponse
        {
            if (_disposed)
            {
                throw new MessageException(
                    $"Protocol error ({request.Command}): Session closed. " +
                    $"Most likely the {_targetType} has been closed." +
                    $"Close reason: {_closeReason}");
            }

            int id = Connection.GetMessageId();
            MessageTask<IFirefoxResponse> callback = null;
            if (waitForCallback)
            {
                callback = new MessageTask<IFirefoxResponse>
                {
                    TaskWrapper = new TaskCompletionSource<IFirefoxResponse>(),
                    Method = request.Command,
                };
                _callbacks[id] = callback;
            }

            try
            {
                await _rawSend(id, (IFirefoxRequest<IFirefoxResponse>)request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (waitForCallback && _callbacks.TryRemove(id, out _))
                {
                    callback.TaskWrapper.TrySetException(new MessageException(ex.Message, ex));
                }
            }

            var result = waitForCallback ? (await callback.TaskWrapper.Task.ConfigureAwait(false)) : null;
            return (TFirefoxResponse)result;
        }

        internal void OnClosed(string reason)
        {
            foreach (var callback in _callbacks)
            {
                callback.Value.TaskWrapper.TrySetException(new PlaywrightSharpException($"Protocol error ({callback.Value.Method}): Target closed. {reason}"));
            }

            _callbacks.Clear();
            _disposed = true;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        internal void OnMessage(ConnectionResponse obj)
        {
            int? id = obj.Id;

            if (id.HasValue && _callbacks.TryRemove(id.Value, out var callback))
            {
                if (obj.Error != null)
                {
                    callback.TaskWrapper.TrySetException(new MessageException(callback, obj.Error));
                }
                else
                {
                    var result = FirefoxProtocolTypes.ParseResponse(callback.Method, obj.Result?.GetRawText());
                    callback.TaskWrapper.TrySetResult(result);
                }
            }
            else if (obj.Params?.ValueKind == JsonValueKind.Object)
            {
                OnMessageReceived(this, FirefoxProtocolTypes.ParseEvent(obj.Method, obj.Params.Value.GetRawText()));
            }
        }

        internal void OnMessageReceived(object sender, IFirefoxEvent e) => MessageReceived?.Invoke(this, e);

        private void OnDisconnected(object sender, TransportClosedEventArgs e) => OnClosed(e.CloseReason);
    }
}
