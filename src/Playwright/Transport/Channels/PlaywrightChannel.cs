using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class PlaywrightChannel : Channel<PlaywrightImpl>
    {
        public PlaywrightChannel(string guid, Connection connection, PlaywrightImpl owner) : base(guid, connection, owner)
        {
        }
    }
}
