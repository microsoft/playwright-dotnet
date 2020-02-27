using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Messaging;

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

        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<EventArgs> Disconnected;

        public bool IsClosed { get; internal set; }

        internal async Task<T> SendAsync<T>(string method, object args = null)
        {
            var content = await SendAsync(method, args).ConfigureAwait(false);
            return content == null ? default : content.Value.ToObject<T>();
        }

        internal async Task<JsonElement?> SendAsync(string method, object args = null, bool waitForCallback = true)
        {
            if (_connection == null)
            {
                throw new MessageException(
                    $"Protocol error ({method}): Session closed. " +
                    $"Most likely the {_targetType} has been closed." +
                    $"Close reason: {_closeReason}");
            }

            int id = _connection.GetMessageId();
            MessageTask callback = null;
            if (waitForCallback)
            {
                callback = new MessageTask
                {
                    TaskWrapper = new TaskCompletionSource<JsonElement?>(),
                    Method = method,
                };
                _callbacks[id] = callback;
            }

            try
            {
                await _connection.RawSendASync(id, method, args, _sessionId).ConfigureAwait(false);
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

            return waitForCallback ? await callback.TaskWrapper.Task.ConfigureAwait(false) : null;
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
                    callback.TaskWrapper.TrySetResult(obj.Result);
                }
            }
            else
            {
                string method = obj.Method;
                var param = obj.Params?.ToObject<ConnectionResponseParams>();

                MessageReceived?.Invoke(this, new MessageEventArgs
                {
                    MessageID = method,
                    MessageData = obj.Params,
                });
            }
        }
    }
}
