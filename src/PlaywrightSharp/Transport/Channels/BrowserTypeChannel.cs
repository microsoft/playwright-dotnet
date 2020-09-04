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
        {
            return Scope.SendMessageToServer<BrowserChannel>(
                Guid,
                "launch",
                options.ToChannelDictionary());
        }

        public Task<BrowserServerChannel> LaunchServerAsync(LaunchOptions options)
            => Scope.SendMessageToServer<BrowserServerChannel>(
                Guid,
                "launchServer",
                (options ?? new LaunchOptions()).ToChannelDictionary());

        internal Task<BrowserChannel> ConnectAsync(string wsEndpoint, int? timeout = null, int? slowMo = null)
            => Scope.SendMessageToServer<BrowserChannel>(
                Guid,
                "connect",
                new Dictionary<string, object>
                {
                    ["wsEndpoint"] = wsEndpoint,
                    ["timeout"] = timeout,
                    ["slowMo"] = slowMo,
                });

        internal Task<BrowserContextChannel> LaunchPersistenContextAsync(string userDataDir, LaunchPersistentOptions options)
        {
            var args = options.ToChannelDictionary();
            args["userDataDir"] = userDataDir;

            return Scope.SendMessageToServer<BrowserContextChannel>(Guid, "launchPersistentContext", args);
        }
    }
}
