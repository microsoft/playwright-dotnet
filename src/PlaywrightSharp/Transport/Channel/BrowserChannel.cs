using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp.Transport.Channel
{
    internal class BrowserChannel : Channel
    {
        public BrowserChannel(string guid, PlaywrightClient client) : base(guid, client)
        {
        }
    }
}
