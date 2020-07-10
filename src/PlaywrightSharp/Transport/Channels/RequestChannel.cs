namespace PlaywrightSharp.Transport.Channels
{
    internal class RequestChannel : Channel<Request>
    {
        public RequestChannel(string guid, ConnectionScope scope, Request owner) : base(guid, scope, owner)
        {
        }
    }
}
