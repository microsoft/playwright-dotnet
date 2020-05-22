using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Target;
using PlaywrightSharp.Messaging;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumSession
    {
        private readonly TargetType _targetType;
        private readonly string _sessionId;
        private readonly string _closeReason = string.Empty;
        private readonly ConcurrentDictionary<int, MessageTask<IChromiumResponse>> _callbacks = new ConcurrentDictionary<int, MessageTask<IChromiumResponse>>();

        public ChromiumSession(ChromiumConnection chromiumConnection, TargetType targetType, string sessionId)
        {
            Connection = chromiumConnection;
            _targetType = targetType;
            _sessionId = sessionId;
        }

        public event EventHandler<IChromiumEvent> MessageReceived;

        public event EventHandler<EventArgs> Disconnected;

        public bool IsClosed { get; internal set; }

        internal ChromiumConnection Connection { get; set; }

        internal async Task<TChromiumResponse> SendAsync<TChromiumResponse>(IChromiumRequest<TChromiumResponse> request, bool waitForCallback = true)
            where TChromiumResponse : IChromiumResponse
        {
            if (Connection == null)
            {
                throw new MessageException(
                    $"Protocol error ({request.Command}): Session closed. " +
                    $"Most likely the {_targetType} has been closed." +
                    $"Close reason: {_closeReason}");
            }

            int id = Connection.GetMessageId();
            MessageTask<IChromiumResponse> callback = null;
            if (waitForCallback)
            {
                callback = new MessageTask<IChromiumResponse>
                {
                    TaskWrapper = new TaskCompletionSource<IChromiumResponse>(),
                    Method = request.Command,
                };
                _callbacks[id] = callback;
            }

            try
            {
                await Connection.RawSendAsync(id, request.Command, request, _sessionId).ConfigureAwait(false);
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
            Connection = null;
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
                    var result = ChromiumProtocolTypes.ParseResponse(callback.Method, obj.Result?.GetRawText());
                    callback.TaskWrapper.TrySetResult(result);
                }
            }
        }

        internal void OnMessageReceived(IChromiumEvent e) => MessageReceived?.Invoke(this, e);

        internal Task DetachAsync()
        {
            if (Connection == null || Connection.IsClosed)
            {
                throw new PlaywrightSharpException($"Session already detached. Most likely the {_targetType} has been closed.");
            }

            return Connection.RootSession.SendAsync(new TargetDetachFromTargetRequest { SessionId = _sessionId });
        }
    }
}
