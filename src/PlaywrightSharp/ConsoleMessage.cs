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
        private readonly ConnectionScope _scope;
        private readonly ConsoleMessageChannel _channel;
        private readonly ConsoleMessageInitializer _initializer;

        internal ConsoleMessage(ConnectionScope scope, string guid, ConsoleMessageInitializer initializer)
        {
            _scope = scope;
            _channel = new ConsoleMessageChannel(guid, scope, this);
            _initializer = initializer;
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
        public ConsoleType Type => _initializer.Type;

        /// <summary>
        /// Gets the arguments.
        /// </summary>
        /// <value>The arguments.</value>
        public IEnumerable<IJSHandle> Args => _initializer.Args.Select(a => a.Object);

        /// <summary>
        /// Gets the location.
        /// </summary>
        public ConsoleMessageLocation Location => _initializer.Location;

        /// <summary>
        /// Gets the console text.
        /// </summary>
        /// <value>The text.</value>
        internal string Text => _initializer.Text;
    }
}
