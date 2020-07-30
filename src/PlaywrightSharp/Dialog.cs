using System;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Dialog : IChannelOwner<Dialog>, IDialog
    {
        private readonly ConnectionScope _scope;
        private readonly DialogChannel _channel;
        private readonly DialogInitializer _initializer;

        public Dialog(ConnectionScope scope, string guid, DialogInitializer initializer)
        {
            _scope = scope;
            _channel = new DialogChannel(guid, scope, this);
            _initializer = initializer;
        }

        /// <inheritdoc/>
        public DialogType DialogType => _initializer.DialogType;

        /// <inheritdoc/>
        public string DefaultValue => _initializer.DefaultValue;

        /// <inheritdoc/>
        public string Message => _initializer.Message;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

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
