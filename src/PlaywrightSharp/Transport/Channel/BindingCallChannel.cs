using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp.Transport.Channel
{
    internal class BindingCallChannel : Channel
    {
        public BindingCallChannel(string guid, PlaywrightClient client) : base(guid, client)
        {
        }
    }
}
