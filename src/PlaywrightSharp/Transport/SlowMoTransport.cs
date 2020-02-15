using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class SlowMoTransport : ITransport
    {
        private readonly int _delay;
        private readonly ITransport _delegate;
        private readonly Queue<string> _incomingMessageQueue = new Queue<string>();

        public SlowMoTransport(ITransport transport, int delay)
        {
            _delay = delay;
            _delegate = transport;

            _delegate.MessageReceived += delegate_MessageReceived;
            _delegate.Closed += delegate_Closed;
        }

        private void delegate_Closed(object sender, TransportClosedEventArgs e)
        {

        }

        private async void delegate_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                _incomingMessageQueue.Enqueue(e.Message);
                ScheduleQueueDispatchAsync();
            }
            catch (Exception ex)
            {
                //TODO Add Logger
                Close($"SlowMoTransport failed to process {e.Message}. {ex.Message}. {ex.StackTrace}");
            }
        }

        private async Task ScheduleQueueDispatchAsync()
        {
            if (this._dispatchTimerId)
                return;
            if (!this._incomingMessageQueue.length)
                return;
            this._dispatchTimerId = setTimeout(() =>
            {
                this._dispatchTimerId = undefined;
                this._dispatchOneMessageFromQueue();
            }, this._delay);
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
        internal static ITransport Wrap(ITransport transport, int delay) => delay > 0 ? new SlowMoTransport(transport, delay) : transport;
    }
}
