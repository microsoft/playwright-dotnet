using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PlaywrightChannel : Channel<Playwright>
    {
        public PlaywrightChannel(string guid, ConnectionScope scope, Playwright owner) : base(guid, scope, owner)
        {
        }
    }
}
