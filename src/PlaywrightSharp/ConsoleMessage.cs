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
    public class ConsoleMessage : ChannelOwnerBase, IChannelOwner<ConsoleMessage>, IConsoleMessage
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

        /// <inheritdoc />
        public string Type => _initializer.Type;

        /// <inheritdoc />
        public IReadOnlyCollection<IJSHandle> Args => _initializer.Args.Select(a => ((JSHandleChannel)a).Object).ToList().AsReadOnly();

        /// <inheritdoc />
        public ConsoleMessageLocationResult Location => _initializer.Location;

        /// <inheritdoc />
        public string Text => _initializer.Text;
    }
}
