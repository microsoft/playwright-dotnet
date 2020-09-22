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
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "accept",
                new Dictionary<string, object>
                {
                    ["promptText"] = promptText,
                });

        internal Task DismissAsync() => Connection.SendMessageToServer<PageChannel>(Guid, "dismiss", null);
    }
}
