using System;
using System.Collections.Generic;
using System.Linq;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <summary>
    /// ConsoleMessage is part of <see cref="ConsoleEventArgs"/> used by <see cref="IPage.Console"/>.
    /// </summary>
    public class ConsoleMessage : IChannelOwner
    {
        private readonly Func<IJSHandle, bool, string> _handleToString;
        private string _text;

        internal ConsoleMessage(PlaywrightClient client, Channel channel, ConsoleMessageInitializer initializer)
        {
            throw new NotImplementedException();
        }

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
        internal string Text
        {
            get
            {
                if (string.IsNullOrEmpty(_text))
                {
                    _text = string.Join(" ", Args.Select(arg => _handleToString(arg, false)).ToArray());
                }

                return _text;
            }
        }
    }
}
