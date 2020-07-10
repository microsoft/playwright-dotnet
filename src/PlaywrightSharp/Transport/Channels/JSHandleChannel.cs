namespace PlaywrightSharp.Transport.Channels
{
    internal class JSHandleChannel : Channel<JSHandle>
    {
        public JSHandleChannel(string guid, ConnectionScope scope, JSHandle owner) : base(guid, scope, owner)
        {
        }
    }
}
