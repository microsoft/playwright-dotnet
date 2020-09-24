using System;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
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
        public DialogType Type => _initializer.Type;

        /// <inheritdoc/>
        public string DefaultValue => _initializer.DefaultValue;

        /// <inheritdoc/>
        public string Message => _initializer.Message;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Dialog> IChannelOwner<Dialog>.Channel => _channel;

        /// <inheritdoc/>
        public Task AcceptAsync(string promptText = "") => _channel.AcceptAsync(promptText);

        /// <inheritdoc/>
        public Task DismissAsync() => _channel.DismissAsync();
    }
}
