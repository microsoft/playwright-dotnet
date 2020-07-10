namespace PlaywrightSharp.Transport.Channels
{
    internal class DownloadChannel : Channel<Download>
    {
        public DownloadChannel(string guid, ConnectionScope scope, Download owner) : base(guid, scope, owner)
        {
        }
    }
}
