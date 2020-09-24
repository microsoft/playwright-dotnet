using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserTypeChannel : Channel<BrowserType>
    {
        public BrowserTypeChannel(string guid, Connection connection, BrowserType owner) : base(guid, connection, owner)
        {
        }

        public Task<BrowserChannel> LaunchAsync(LaunchOptions options)
            => Connection.SendMessageToServer<BrowserChannel>(
                Guid,
                "launch",
                options.ToChannelDictionary(),
                false);

        internal Task<BrowserContextChannel> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options)
        {
            var args = options.ToChannelDictionary();
            args["userDataDir"] = userDataDir;

            return Connection.SendMessageToServer<BrowserContextChannel>(Guid, "launchPersistentContext", args, false);
        }
    }
}
