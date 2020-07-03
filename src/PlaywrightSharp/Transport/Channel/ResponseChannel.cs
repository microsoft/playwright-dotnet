namespace PlaywrightSharp.Transport.Channel
{
    internal class ResponseChannel : Channel
    {
        public ResponseChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
