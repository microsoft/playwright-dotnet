using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumConnection : IDisposable
    {
        private readonly AsyncDictionaryHelper<string, ChromiumSession> _asyncSessions;
        private readonly ConcurrentDictionary<int, MessageTask> _callbacks = new ConcurrentDictionary<int, MessageTask>();
        private readonly IConnectionTransport _transport;
        private readonly ConcurrentDictionary<string, ChromiumSession> _sessions = new ConcurrentDictionary<string, ChromiumSession>();
        private int _lastId;

        public ChromiumConnection(IConnectionTransport transport)
        {
            _transport = transport;
            _transport.MessageReceived += Transport_MessageReceived;
            _transport.Closed += Transport_Closed;
            RootSession = new ChromiumSession(this, TargetType.Browser, string.Empty);
            _sessions.TryAdd(string.Empty, RootSession);

            _asyncSessions = new AsyncDictionaryHelper<string, ChromiumSession>(_sessions, "Session {0} not found");
        }

        public event EventHandler<MessageEventArgs> MessageReceived;

        public ChromiumSession RootSession { get; set; }

        public void Dispose()
        {
        }

        internal int GetMessageId() => Interlocked.Increment(ref _lastId);

        internal Task RawSendASync(int id, string method, object args, string sessionId)
            => _transport.SendAsync(JsonSerializer.Serialize(new ConnectionRequest
            {
                Id = id,
                Method = method,
                Params = args,
                SessionId = sessionId,
            }));

        internal ChromiumSession GetSession(string sessionId) => _sessions.GetValueOrDefault(sessionId);

        private void Transport_Closed(object sender, TransportClosedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Transport_MessageReceived(object sender, MessageReceivedEventArgs e) => ProcessMessage(e);

        private void ProcessMessage(MessageReceivedEventArgs e)
        {
            try
            {
                string response = e.Message;
                ConnectionResponse obj = null;

                try
                {
                    obj = JsonSerializer.Deserialize<ConnectionResponse>(response);
                }
                catch (JsonException)
                {
                    // _logger.LogError(exc, "Failed to deserialize response", response);
                    return;
                }

                // _logger.LogTrace("◀ Receive {Message}", response);
                ProcessIncomingMessage(obj);
            }

            // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types.
            {
                string message = $"Connection failed to process {e.Message}. {ex.Message}. {ex.StackTrace}";

                // _logger.LogError(ex, message);
                Close(message);
            }
        }

        private void Close(string message)
        {
            throw new NotImplementedException();
        }

        private void ProcessIncomingMessage(ConnectionResponse obj)
        {
            string method = obj.Method;
            var param = obj.Params?.ToObject<ConnectionResponseParams>();

            if (method == "Target.attachedToTarget")
            {
                string sessionId = param.SessionId;
                var session = new ChromiumSession(this, param.TargetInfo.Type, sessionId);
                _asyncSessions.AddItem(sessionId, session);
            }
            else if (method == "Target.detachedFromTarget")
            {
                string sessionId = param.SessionId;
                if (_sessions.TryRemove(sessionId, out var session) && !session.IsClosed)
                {
                    session.Close("Target.detachedFromTarget");
                }
            }

            if (!string.IsNullOrEmpty(obj.SessionId))
            {
                var session = GetSession(obj.SessionId);
                session.OnMessage(obj);
            }
            else if (obj.Id.HasValue)
            {
                // If we get the object we are waiting for we return if
                // if not we add this to the list, sooner or later some one will come for it.
                if (_callbacks.TryRemove(obj.Id.Value, out var callback))
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
            }
            else
            {
                MessageReceived?.Invoke(this, new MessageEventArgs
                {
                    MessageID = method,
                    MessageData = obj.Params,
                });
            }
        }
    }
}
