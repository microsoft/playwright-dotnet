using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumConnection
    {
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
        }

        public ChromiumSession RootSession { get; set; }

        internal int GetMessageId() => Interlocked.Increment(ref _lastId);

        internal Task RawSendASync(int id, string method, object args, string sessionId)
        {
            throw new NotImplementedException();
        }

        private void Transport_Closed(object sender, TransportClosedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Transport_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
