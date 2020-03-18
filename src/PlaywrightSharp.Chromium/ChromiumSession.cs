using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Chromium.Protocol;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumSession
    {
        private readonly TargetType _targetType;
        private readonly string _sessionId;
        private readonly string _closeReason = string.Empty;
        private readonly ConcurrentDictionary<int, MessageTask> _callbacks = new ConcurrentDictionary<int, MessageTask>();
        private ChromiumConnection _connection;

        public ChromiumSession(ChromiumConnection chromiumConnection, TargetType targetType, string sessionId)
        {
            _connection = chromiumConnection;
            _targetType = targetType;
            _sessionId = sessionId;

            chromiumConnection.MessageReceived += OnMessageReceived;
        }

        public event EventHandler<IChromiumEvent> MessageReceived;

        public event EventHandler<EventArgs> Disconnected;

        public bool IsClosed { get; internal set; }

        internal async Task<TChromiumResponse> SendAsync<TChromiumResponse>(IChromiumRequest<TChromiumResponse> request, bool waitForCallback = true)
            where TChromiumResponse : IChromiumResponse
        {
            if (_connection == null)
            {
                throw new MessageException(
                    $"Protocol error ({request.Command}): Session closed. " +
                    $"Most likely the {_targetType} has been closed." +
                    $"Close reason: {_closeReason}");
            }

            int id = _connection.GetMessageId();
            MessageTask callback = null;
            if (waitForCallback)
            {
                callback = new MessageTask
                {
                    TaskWrapper = new TaskCompletionSource<IChromiumResponse>(),
                    Method = request.Command,
                };
                _callbacks[id] = callback;
            }

            try
            {
                await _connection.RawSendASync(id, request.Command, request, _sessionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (waitForCallback && _callbacks.TryRemove(id, out _))
                {
                    callback.TaskWrapper.TrySetException(new MessageException(ex.Message, ex));
                }
            }

            var result = waitForCallback ? (await callback.TaskWrapper.Task.ConfigureAwait(false)) : null;
            return (TChromiumResponse)result;
        }

        internal void OnClosed(string reason)
        {
            foreach (var callback in _callbacks)
            {
                callback.Value.TaskWrapper.TrySetException(new TargetClosedException($"Protocol error ({callback.Value.Method}): Target closed. {reason}"));
            }

            _callbacks.Clear();
            _connection = null;
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
                    var result = ChromiumProtocolTypes.ParseResponse(callback.Method, obj.Result.Value.GetRawText());
                    callback.TaskWrapper.TrySetResult(result);
                }
            }
        }

        internal void OnMessageReceived(object sender, IChromiumEvent e)
        {
            MessageReceived?.Invoke(this, e);
        }

        internal Task DetachAsync()
        {
            throw new NotImplementedException();
        }
    }
}
