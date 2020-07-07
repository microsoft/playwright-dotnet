using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserTypeChannel : Channel
    {
        public BrowserTypeChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }

        public Task<BrowserChannel> LaunchAsync(LaunchOptions options)
            => Scope.SendMessageToServer<BrowserChannel>(
                Guid,
                "launch",
                new LaunchRequest { Options = new LaunchOptionsRequest { Headless = false } });

        internal class LaunchRequest
        {
            public LaunchOptionsRequest Options { get; set; }
        }

        internal class LaunchOptionsRequest
        {
            public bool Headless { get; set; }
        }
    }
}
