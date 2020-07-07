namespace PlaywrightSharp.Transport.Channels
{
    internal class RequestChannel : Channel
    {
        public RequestChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
