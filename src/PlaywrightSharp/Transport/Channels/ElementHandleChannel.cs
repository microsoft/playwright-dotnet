namespace PlaywrightSharp.Transport.Channels
{
    internal class ElementHandleChannel : Channel<ElementHandle>
    {
        public ElementHandleChannel(string guid, ConnectionScope scope, ElementHandle owner) : base(guid, scope, owner)
        {
        }
    }
}
