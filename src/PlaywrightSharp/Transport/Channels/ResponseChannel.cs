namespace PlaywrightSharp.Transport.Channels
{
    internal class ResponseChannel : Channel
    {
        public ResponseChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
