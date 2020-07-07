namespace PlaywrightSharp.Transport.Channels
{
    internal class ConsoleMessageChannel : Channel
    {
        public ConsoleMessageChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
