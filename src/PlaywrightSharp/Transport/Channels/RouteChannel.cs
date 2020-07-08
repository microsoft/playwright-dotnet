namespace PlaywrightSharp.Transport.Channels
{
    internal class RouteChannel : Channel<Route>
    {
        public RouteChannel(string guid, ConnectionScope scope, Route owner) : base(guid, scope, owner)
        {
        }
    }
}
