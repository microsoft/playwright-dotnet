using PlaywrightSharp.Transport;

namespace PlaywrightSharp
{
    internal class Connection
    {
        public Connection(IConnectionTransport transport)
        {
            transport.MessageReceived += TransportOnMessageReceived;
        }

        private void TransportOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
