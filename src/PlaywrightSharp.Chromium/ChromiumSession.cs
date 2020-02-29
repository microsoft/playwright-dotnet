using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Chromium.Protocol;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumSession
    {
        private readonly ChromiumConnection _connection;
        private readonly TargetType _targetType;
        private readonly string _sessionId;
        private readonly string _closeReason = string.Empty;

        private readonly ConcurrentDictionary<int, MessageTask> _callbacks = new ConcurrentDictionary<int, MessageTask>();

        public ChromiumSession(ChromiumConnection chromiumConnection, TargetType targetType, string sessionId)
        {
            _connection = chromiumConnection;
            _targetType = targetType;
            _sessionId = sessionId;
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

            // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types.
            {
                if (waitForCallback && _callbacks.TryRemove(id, out _))
                {
                    callback.TaskWrapper.TrySetException(new MessageException(ex.Message, ex));
                }
            }

            var result = waitForCallback ? (await callback.TaskWrapper.Task.ConfigureAwait(false)) : null;
            return (TChromiumResponse)result;
        }

        internal void Close(string reason)
        {
            throw new NotImplementedException();
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
                    var result = ChromiumProtocolTypes.ParseResponse(callback.Method, obj.Result);
                    callback.TaskWrapper.TrySetResult(result);
                }
            }
            else
            {
                var data = ChromiumProtocolTypes.ParseEvent(obj.Params, obj.Method);
                MessageReceived?.Invoke(this, data);
            }
        }
    }
}
