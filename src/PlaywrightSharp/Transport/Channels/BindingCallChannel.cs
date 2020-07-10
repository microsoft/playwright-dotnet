namespace PlaywrightSharp.Transport.Channels
{
    internal class BindingCallChannel : Channel<BindingCall>
    {
        public BindingCallChannel(string guid, ConnectionScope scope, BindingCall owner) : base(guid, scope, owner)
        {
        }
    }
}
