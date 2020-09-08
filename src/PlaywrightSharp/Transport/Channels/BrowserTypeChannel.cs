using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserTypeChannel : Channel<BrowserType>
    {
        public BrowserTypeChannel(string guid, ConnectionScope scope, BrowserType owner) : base(guid, scope, owner)
        {
        }

        public Task<BrowserChannel> LaunchAsync(LaunchOptions options)
            => Scope.SendMessageToServer<BrowserChannel>(
                Guid,
                "launch",
                options.ToChannelDictionary(),
                false);

        internal Task<BrowserContextChannel> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options)
        {
            var args = options.ToChannelDictionary();
            args["userDataDir"] = userDataDir;

            return Scope.SendMessageToServer<BrowserContextChannel>(Guid, "launchPersistentContext", args, false);
        }
    }
}
