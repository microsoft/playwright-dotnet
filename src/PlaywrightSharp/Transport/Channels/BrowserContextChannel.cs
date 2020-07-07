namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextChannel : Channel
    {
        public BrowserContextChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
