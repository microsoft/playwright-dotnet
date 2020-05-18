using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Target;
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

        public event EventHandler Disconnected;

        public ChromiumSession RootSession { get; set; }

        internal bool IsClosed { get; set; }

        public void Dispose()
        {
        }

        internal static ChromiumConnection FromSession(ChromiumSession client) => client.Connection;

        internal int GetMessageId() => Interlocked.Increment(ref _lastId);

        internal Task RawSendAsync(int id, string method, object args, string sessionId)
            => _transport.SendAsync(new ConnectionRequest
            {
                Id = id,
                Method = method,
                Params = args,
                SessionId = string.IsNullOrEmpty(sessionId) ? null : sessionId,
            }.ToJson());

        internal ChromiumSession GetSession(string sessionId) => _sessions.GetValueOrDefault(sessionId);

        internal void Close(string closeReason)
        {
            if (!IsClosed)
            {
                _transport.Close(closeReason);
            }
        }

        internal Task<ChromiumSession> GetSessionAsync(string sessionId) => _asyncSessions.GetItemAsync(sessionId);

        internal async Task<ChromiumSession> CreateSessionAsync(Protocol.Target.TargetInfo targetInfo)
        {
            string sessionId = (await RootSession.SendAsync(new TargetAttachToTargetRequest
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
                ConnectionResponse obj;

                try
                {
                    obj = JsonSerializer.Deserialize<ConnectionResponse>(response, JsonHelper.DefaultJsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    // _logger.LogError(ex, "Failed to deserialize response", response);
                    System.Diagnostics.Debug.WriteLine($"{ex}: Failed to deserialize response {response}");
                    return;
                }

                // _logger.LogTrace("◀ Receive {Message}", response);
                ProcessIncomingMessage(obj);
            }
            catch (Exception ex)
            {
                string message = $"Connection failed to process {e.Message}. {ex.Message}. {ex.StackTrace}";

                // _logger.LogError(ex, message);
                System.Diagnostics.Debug.WriteLine(message);
                Close(message);
            }
        }

        private void ProcessIncomingMessage(ConnectionResponse obj)
        {
            if (!obj.Params.HasValue)
            {
                GetSession(obj.SessionId ?? string.Empty)?.OnMessage(obj);
                return;
            }

            var param = ChromiumProtocolTypes.ParseEvent(obj.Method, obj.Params.Value.GetRawText());
            if (obj.Id == BrowserCloseMessageId)
            {
                return;
            }

            if (param is TargetAttachedToTargetChromiumEvent targetAttachedToTarget)
            {
                string sessionId = targetAttachedToTarget.SessionId;
                ChromiumSession session = new ChromiumSession(this, targetAttachedToTarget.TargetInfo.GetTargetType(), sessionId);
                _asyncSessions.AddItem(sessionId, session);
            }
            else if (param is TargetDetachedFromTargetChromiumEvent targetDetachedFromTarget)
            {
                string sessionId = targetDetachedFromTarget.SessionId;
                if (_sessions.TryRemove(sessionId, out var session) && !session.IsClosed)
                {
                    session.OnClosed(targetDetachedFromTarget.InternalName);
                }
            }

            GetSession(obj.SessionId ?? string.Empty).OnMessageReceived(param);
        }
    }
}
