namespace PlaywrightSharp.Transport.Channels
{
    internal class ConsoleMessageChannel : Channel<ConsoleMessage>
    {
        public ConsoleMessageChannel(string guid, ConnectionScope scope, ConsoleMessage owner) : base(guid, scope, owner)
        {
        }
    }
}
