using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class WebsocketTransport : ITransport
    {
        public WebsocketTransport()
        {
        }

        /// <inheritdoc cref="ITransport"/>
        public event EventHandler<TransportClosedEventArgs> Closed;

        /// <inheritdoc cref="ITransport"/>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <inheritdoc cref="ITransport"/>
        public void Close()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="ITransport"/>
        public Task SendAsync(string message)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="ITransport"/>
        internal static Task<ITransport> CreateAsync(string browserWSEndpoint)
        {
            throw new NotImplementedException();
        }
    }
}
