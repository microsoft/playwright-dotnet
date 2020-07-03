using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channel
{
    internal class BrowserTypeChannel : Channel
    {
        public BrowserTypeChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }

        public Task<BrowserChannel> LaunchAsync(LaunchOptions options)
        {
            throw new System.NotImplementedException();
        }
    }
}
