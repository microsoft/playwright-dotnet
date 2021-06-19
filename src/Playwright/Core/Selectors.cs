using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal class Selectors : ChannelOwnerBase, IChannelOwner<Selectors>, ISelectors
    {
        private readonly SelectorsChannel _channel;

        internal Selectors(IChannelOwner parent, string guid) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Selectors> IChannelOwner<Selectors>.Channel => _channel;

        public async Task RegisterAsync(string name, SelectorsRegisterOptions options = default)
        {
            options ??= new SelectorsRegisterOptions();

            var script = ScriptsHelper.EvaluationScript(options?.Script, options?.Path);
            var registerParam = new SelectorsRegisterParams
            {
                Name = name,
                Source = script,
                ContentScript = options?.ContentScript,
            };

            await _channel.RegisterAsync(registerParam).ConfigureAwait(false);
        }
    }
}
