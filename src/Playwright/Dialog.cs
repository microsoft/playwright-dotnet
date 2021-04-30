using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    internal class Dialog : ChannelOwnerBase, IChannelOwner<Dialog>, IDialog
    {
        private readonly DialogChannel _channel;
        private readonly DialogInitializer _initializer;

        public Dialog(IChannelOwner parent, string guid, DialogInitializer initializer) : base(parent, guid)
        {
            _channel = new DialogChannel(guid, parent.Connection, this);
            _initializer = initializer;
        }

        /// <inheritdoc/>
        public string Type => _initializer.Type;

        /// <inheritdoc/>
        public string DefaultValue => _initializer.DefaultValue;

        /// <inheritdoc/>
        public string Message => _initializer.Message;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Dialog> IChannelOwner<Dialog>.Channel => _channel;

        /// <inheritdoc/>
        public Task AcceptAsync(string promptText) => _channel.AcceptAsync(promptText ?? string.Empty);

        /// <inheritdoc/>
        public Task DismissAsync() => _channel.DismissAsync();
    }
}
