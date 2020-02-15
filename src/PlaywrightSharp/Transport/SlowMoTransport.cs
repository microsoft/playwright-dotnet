using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class SlowMoTransport : ITransport
    {
        public SlowMoTransport()
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
        internal static ITransport Wrap(ITransport transport, int slowMo)
        {
            throw new NotImplementedException();
        }
    }
}
