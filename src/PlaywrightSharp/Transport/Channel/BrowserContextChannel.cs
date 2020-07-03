namespace PlaywrightSharp.Transport.Channel
{
    internal class BrowserContextChannel : Channel
    {
        public BrowserContextChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
