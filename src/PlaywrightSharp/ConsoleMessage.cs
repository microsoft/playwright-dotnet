using System;
using System.Collections.Generic;
using System.Linq;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <summary>
    /// ConsoleMessage is part of <see cref="ConsoleEventArgs"/> used by <see cref="IPage.Console"/>.
    /// </summary>
    public class ConsoleMessage : IChannelOwner<ConsoleMessage>
    {
        private ConnectionScope _scope;
        private ConsoleMessageChannel _channel;

        internal ConsoleMessage(ConnectionScope scope, string guid, ConsoleMessageInitializer initializer)
        {
            _scope = scope;
            _channel = new ConsoleMessageChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<ConsoleMessage> IChannelOwner<ConsoleMessage>.Channel => _channel;

        /// <summary>
        /// Gets the ConsoleMessage type.
        /// </summary>
        /// <value>ConsoleMessageType.</value>
        public ConsoleType Type { get; }

        /// <summary>
        /// Gets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        public IEnumerable<IJSHandle> Args { get; }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public ConsoleMessageLocation Location { get; }

        /// <summary>
        /// Gets the console text.
        /// </summary>
        /// <value>The text.</value>
        internal string Text { get; }
    }
}
