using System;
using System.Runtime.Serialization;
using PlaywrightSharp.Chromium.Messaging;

namespace PlaywrightSharp.Chromium
{
    [Serializable]
    internal class MessageException : PlaywrightSharpException
    {
        public MessageException()
        {
        }

        public MessageException(string message) : base(message)
        {
        }

        public MessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal MessageException(MessageTask callback, ConnectionError error) : base(GetCallbackMessage(callback, error))
        {
        }

        protected MessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal static string GetCallbackMessage(MessageTask callback, ConnectionError connectionError)
        {
            string message = $"Protocol error ({callback.Method}): {connectionError.Message}";

            if (!string.IsNullOrEmpty(connectionError.Data))
            {
                message += $" {connectionError.Data}";
            }

            return !string.IsNullOrEmpty(connectionError.Message) ? RewriteErrorMeesage(message) : string.Empty;
        }
    }
}
