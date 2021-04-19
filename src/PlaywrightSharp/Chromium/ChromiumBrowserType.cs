using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="BrowserType"/>
    public class ChromiumBrowserType : BrowserType
    {
        internal ChromiumBrowserType(IChannelOwner parent, string guid, BrowserTypeInitializer initializer) : base(parent, guid, initializer)
        {
        }
    }
}
