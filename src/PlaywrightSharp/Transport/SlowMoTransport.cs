using System;
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

        /// <inheritdoc cref="IConnectionTransport.Closed"/>
        public event EventHandler<TransportClosedEventArgs> Closed;

        /// <inheritdoc cref="IConnectionTransport.MessageReceived"/>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <inheritdoc cref="IConnectionTransport.SendAsync(string, object)"/>
        public Task SendAsync(string message, object arguments = null) => _delegate.SendAsync(message, arguments);

        /// <inheritdoc cref="IConnectionTransport.Close(string)"/>
        public void Close(string closeReason = null)
        {
            throw new NotImplementedException();
        }

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
            catch (Exception ex)
            {
                // TODO Add Logger
                Close($"SlowMoTransport failed to process {e.Message}. {ex}");
            }
        }
    }
}
