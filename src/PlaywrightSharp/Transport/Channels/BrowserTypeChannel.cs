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
                new Dictionary<string, object>
                {
                    ["options"] = options ?? new LaunchOptions(),
                });
    }
}
