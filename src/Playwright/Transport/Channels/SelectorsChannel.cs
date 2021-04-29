using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class SelectorsChannel : Channel<SelectorsOwner>
    {
        public SelectorsChannel(string guid, Connection connection, SelectorsOwner owner) : base(guid, connection, owner)
        {
        }

        internal Task RegisterAsync(SelectorsRegisterParams registration)
            => Connection.SendMessageToServerAsync(Guid, "register", registration);
    }
}
