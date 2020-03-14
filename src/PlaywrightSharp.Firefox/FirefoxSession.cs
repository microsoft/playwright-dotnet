using System;
using PlaywrightSharp.Firefox.Messaging;
using PlaywrightSharp.Firefox.Protocol.Target;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxSession
    {
        private readonly FirefoxConnection _firefoxConnection;
        private readonly TargetInfoType _type;
        private readonly string _sessionId;

        public FirefoxSession(FirefoxConnection firefoxConnection, TargetInfoType type, string sessionId)
        {
            _firefoxConnection = firefoxConnection;
            _type = type;
            _sessionId = sessionId;
        }

        internal void OnMessage(ConnectionResponse obj)
        {
            throw new NotImplementedException();
        }

        internal void OnClosed(string internalName)
        {
            throw new NotImplementedException();
        }
    }
}
