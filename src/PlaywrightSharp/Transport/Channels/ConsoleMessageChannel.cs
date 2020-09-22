namespace PlaywrightSharp.Transport.Channels
{
    internal class ConsoleMessageChannel : Channel<ConsoleMessage>
    {
        public ConsoleMessageChannel(string guid, Connection connection, ConsoleMessage owner) : base(guid, connection, owner)
        {
        }
    }
}
