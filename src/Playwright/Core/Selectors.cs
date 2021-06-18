using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal partial class Selectors : ChannelOwnerBase, IChannelOwner<Selectors>, ISelectors
    {
        private readonly SelectorsChannel _channel;

        internal Selectors(IChannelOwner parent, string guid) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Selectors> IChannelOwner<Selectors>.Channel => _channel;

        public Task RegisterAsync(string name, string script, string path, bool? contentScript = null)
        {
            script = ScriptsHelper.EvaluationScript(script, path);

            var registerParam = new SelectorsRegisterParams
            {
                Name = name,
                Source = script,
                ContentScript = contentScript,
            };
            return _channel.RegisterAsync(registerParam);
        }
    }
}
