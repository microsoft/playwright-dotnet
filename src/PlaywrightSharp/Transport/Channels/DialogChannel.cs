using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class DialogChannel : Channel<Dialog>
    {
        public DialogChannel(string guid, Connection connection, Dialog owner) : base(guid, connection, owner)
        {
        }

        internal Task AcceptAsync(string promptText)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "accept",
                new Dictionary<string, object>
                {
                    ["promptText"] = promptText,
                });

        internal Task DismissAsync() => Connection.SendMessageToServerAsync<PageChannel>(Guid, "dismiss", null);
    }
}
