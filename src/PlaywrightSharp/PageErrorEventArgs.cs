using System;

namespace PlaywrightSharp
{
    internal class PageErrorEventArgs
    {
        private string _message;

        /// <summary>
        /// Error name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Error Message.
        /// </summary>
        public string Message
        {
            get => _message ?? Value;
            set => _message = value;
        }

        /// <summary>
        /// Error Value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Error stack.
        /// </summary>
        public string Stack { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            string result = $"{Name}: {Message}";
            if (!string.IsNullOrEmpty(Stack))
            {
                result += $"\n{result}";
            }

            return result;
        }
    }
}
