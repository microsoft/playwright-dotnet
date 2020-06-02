using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp
{
    /// <summary>
    /// ConsoleMessage is part of <see cref="ConsoleEventArgs"/> used by <see cref="IPage.Console"/>.
    /// </summary>
    public class ConsoleMessage
    {
        private readonly Func<IJSHandle, bool, string> _handleToString;
        private string _text;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleMessage"/> class.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="text">Text.</param>
        /// <param name="args">Arguments.</param>
        /// <param name="handleToString">IJSHandle to string.</param>
        /// <param name="location">Message location.</param>
        internal ConsoleMessage(ConsoleType type, string text, IEnumerable<IJSHandle> args, Func<IJSHandle, bool, string> handleToString, ConsoleMessageLocation location = null)
        {
            Type = type;
            _text = text;
            _handleToString = handleToString;
            Args = args;
            Location = location;
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
