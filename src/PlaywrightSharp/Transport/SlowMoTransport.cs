using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class SlowMoTransport : IConnectionTransport
    {
        private readonly int _delay;
        private readonly IConnectionTransport _delegate;

        public SlowMoTransport(IConnectionTransport transport, int delay)
        {
            _delay = delay;
            _delegate = transport;
            _delegate.MessageReceived += Delegate_MessageReceived;
            _delegate.Closed += Delegate_Closed;
        }

        /// <inheritdoc cref="IConnectionTransport"/>
        public event EventHandler<TransportClosedEventArgs> Closed;

        /// <inheritdoc cref="IConnectionTransport"/>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <inheritdoc cref="IConnectionTransport"/>
        public Task SendAsync(string message) => _delegate.SendAsync(message);

        /// <inheritdoc cref="IConnectionTransport"/>
        public void Close(string closeReason = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IConnectionTransport"/>
        internal static IConnectionTransport Wrap(IConnectionTransport transport, int delay) => delay > 0 ? new SlowMoTransport(transport, delay) : transport;

        private void Delegate_Closed(object sender, TransportClosedEventArgs e)
        {
            Closed?.Invoke(this, e);
            _delegate.MessageReceived -= Delegate_MessageReceived;
            _delegate.Closed -= Delegate_Closed;
        }

        private async void Delegate_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                await Task.Delay(_delay).ConfigureAwait(false);
                MessageReceived?.Invoke(this, e);
            }

            // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // TODO Add Logger
                Close($"SlowMoTransport failed to process {e.Message}. {ex.Message}. {ex.StackTrace}");
            }
        }
    }
}
