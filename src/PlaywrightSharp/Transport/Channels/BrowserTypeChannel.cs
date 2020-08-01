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
                (options ?? new LaunchOptions()).ToChannelDictionary());

        public Task<BrowserServerChannel> LaunchServerAsync(LaunchOptions options)
            => Scope.SendMessageToServer<BrowserServerChannel>(
                Guid,
                "launchServer",
                (options ?? new LaunchOptions()).ToChannelDictionary());

        internal Task<BrowserChannel> ConnectAsync(ConnectOptions options)
            => Scope.SendMessageToServer<BrowserChannel>(
                Guid,
                "connect",
                options ?? new ConnectOptions());

        internal Task<BrowserContextChannel> LaunchPersistenContextAsync(string userDataDir, LaunchPersistentOptions options)
        {
            var args = (options ?? new LaunchPersistentOptions()).ToChannelDictionary();
            args["userDataDir"] = userDataDir;

            return Scope.SendMessageToServer<BrowserContextChannel>(Guid, "launchPersistentContext", args);
        }
    }
}
