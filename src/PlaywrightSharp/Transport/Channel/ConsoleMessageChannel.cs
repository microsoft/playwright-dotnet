namespace PlaywrightSharp.Transport.Channel
{
    internal class ConsoleMessageChannel : Channel
    {
        public ConsoleMessageChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
