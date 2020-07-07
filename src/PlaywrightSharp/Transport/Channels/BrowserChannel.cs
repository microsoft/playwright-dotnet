namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserChannel : Channel
    {
        public BrowserChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
