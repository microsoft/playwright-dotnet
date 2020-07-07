namespace PlaywrightSharp.Transport.Channels
{
    internal class DownloadChannel : Channel
    {
        public DownloadChannel(string guid, ConnectionScope scope) : base(guid, scope)
        {
        }
    }
}
