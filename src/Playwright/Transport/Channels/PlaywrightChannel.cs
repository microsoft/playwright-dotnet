using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class PlaywrightChannel : Channel<PlaywrightImpl>
    {
        public PlaywrightChannel(string guid, Connection connection, PlaywrightImpl owner) : base(guid, connection, owner)
        {
        }
    }
}
