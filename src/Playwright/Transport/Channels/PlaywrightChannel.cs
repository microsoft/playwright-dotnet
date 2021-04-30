using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class PlaywrightChannel : Channel<Playwright>
    {
        public PlaywrightChannel(string guid, Connection connection, Playwright owner) : base(guid, connection, owner)
        {
        }
    }
}
