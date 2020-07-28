using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class DialogChannel : Channel<Dialog>
    {
        public DialogChannel(string guid, ConnectionScope scope, Dialog owner) : base(guid, scope, owner)
        {
        }

        internal Task AcceptAsync(string promptText)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "accept",
                new Dictionary<string, object>
                {
                    ["promptText"] = promptText,
                });

        internal Task DismissAsync() => Scope.SendMessageToServer<PageChannel>(Guid, "dismiss", null);
    }
}
