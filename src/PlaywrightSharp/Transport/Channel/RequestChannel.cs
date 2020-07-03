namespace PlaywrightSharp.Transport.Channel
{
    internal class RequestChannel : Channel
    {
        public RequestChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
