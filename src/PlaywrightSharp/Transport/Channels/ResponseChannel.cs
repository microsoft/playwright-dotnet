namespace PlaywrightSharp.Transport.Channels
{
    internal class ResponseChannel : Channel<Response>
    {
        public ResponseChannel(string guid, ConnectionScope scope, Response owner) : base(guid, scope, owner)
        {
        }
    }
}
