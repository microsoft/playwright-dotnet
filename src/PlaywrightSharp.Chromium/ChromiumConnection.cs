using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Chromium.Messaging.Target;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumConnection : IDisposable
    {
        internal const int BrowserCloseMessageId = -9999;

        private readonly AsyncDictionaryHelper<string, ChromiumSession> _asyncSessions;
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

        public event EventHandler Disconnected;

        public ChromiumSession RootSession { get; set; }

        internal bool IsClosed { get; set; }

        public void Dispose()
        {
        }

        internal int GetMessageId() => Interlocked.Increment(ref _lastId);

        internal Task RawSendASync(int id, string method, object args, string sessionId)
            => _transport.SendAsync(JsonSerializer.Serialize(
                new ConnectionRequest
                {
                    Id = id,
                    Method = method,
                    Params = args,
                    SessionId = string.IsNullOrEmpty(sessionId) ? null : sessionId,
                },
                JsonHelper.DefaultChromiumJsonSerializerOptions));

        internal ChromiumSession GetSession(string sessionId) => _sessions.GetValueOrDefault(sessionId);

        internal void Close(string closeReason)
        {
            if (!IsClosed)
            {
                _transport.Close(closeReason);
            }
        }

        internal Task<ChromiumSession> GetSessionAsync(string sessionId) => _asyncSessions.GetItemAsync(sessionId);

        internal async Task<ChromiumSession> CreateSessionAsync(TargetInfo targetInfo)
        {
            string sessionId = (await RootSession.SendAsync<TargetAttachToTargetResponse>("Target.attachToTarget", new TargetAttachToTargetRequest
            {
                TargetId = targetInfo.TargetId,
                Flatten = true,
            }).ConfigureAwait(false)).SessionId;
            return await GetSessionAsync(sessionId).ConfigureAwait(false);
        }

        private void Transport_Closed(object sender, TransportClosedEventArgs e) => Disconnected?.Invoke(this, e);

        private void Transport_MessageReceived(object sender, MessageReceivedEventArgs e) => ProcessMessage(e);

        private void ProcessMessage(MessageReceivedEventArgs e)
        {
            try
            {
                string response = e.Message;
                ConnectionResponse obj = null;

                try
                {
                    obj = JsonSerializer.Deserialize<ConnectionResponse>(response, JsonHelper.DefaultChromiumJsonSerializerOptions);
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

        private void ProcessIncomingMessage(ConnectionResponse obj)
        {
            string method = obj.Method;
            var param = obj.Params?.ToObject<ConnectionResponseParams>();
            ChromiumSession session;

            if (obj.Id == BrowserCloseMessageId)
            {
                return;
            }

            if (method == "Target.attachedToTarget")
            {
                string sessionId = param.SessionId;
                session = new ChromiumSession(this, param.TargetInfo.Type, sessionId);
                _asyncSessions.AddItem(sessionId, session);

                return;
            }

            if (method == "Target.detachedFromTarget")
            {
                string sessionId = param.SessionId;
                if (_sessions.TryRemove(sessionId, out session) && !session.IsClosed)
                {
                    session.Close("Target.detachedFromTarget");
                }

                return;
            }

            GetSession(obj.SessionId ?? string.Empty)?.OnMessage(obj);
        }
    }
}
