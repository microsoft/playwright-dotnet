using System.Threading.Tasks;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class SelectorsChannel : Channel<Selectors>
    {
        public SelectorsChannel(string guid, Connection connection, Selectors owner) : base(guid, connection, owner)
        {
        }

        internal Task RegisterAsync(SelectorsRegisterParams registration)
            => Connection.SendMessageToServerAsync(Guid, "register", registration);
    }
}
