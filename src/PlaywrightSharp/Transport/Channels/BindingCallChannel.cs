namespace PlaywrightSharp.Transport.Channels
{
    internal class BindingCallChannel : Channel
    {
        public BindingCallChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
