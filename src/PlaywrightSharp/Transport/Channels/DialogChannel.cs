namespace PlaywrightSharp.Transport.Channels
{
    internal class DialogChannel : Channel<Dialog>
    {
        public DialogChannel(string guid, ConnectionScope scope, Dialog owner) : base(guid, scope, owner)
        {
        }
    }
}
