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
    public class ConsoleMessage : ChannelOwnerBase, IChannelOwner<ConsoleMessage>
    {
        private readonly ConsoleMessageChannel _channel;
        private readonly ConsoleMessageInitializer _initializer;

        internal ConsoleMessage(IChannelOwner parent, string guid, ConsoleMessageInitializer initializer) : base(parent, guid)
        {
            _channel = new ConsoleMessageChannel(guid, parent.Connection, this);
            _initializer = initializer;
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<ConsoleMessage> IChannelOwner<ConsoleMessage>.Channel => _channel;

        /// <summary>
        /// Gets the ConsoleMessage type.
        /// </summary>
        public string Type => _initializer.Type;

        /// <summary>
        /// Gets the arguments.
        /// </summary>
        public IEnumerable<IJSHandle> Args => _initializer.Args.Select(a => ((JSHandleChannel)a).Object);

        /// <summary>
        /// Gets the location.
        /// </summary>
        public ConsoleMessageLocation Location => _initializer.Location;

        /// <summary>
        /// Gets the console text.
        /// </summary>
        public string Text => _initializer.Text;
    }
}
