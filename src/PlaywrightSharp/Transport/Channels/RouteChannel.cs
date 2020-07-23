using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RouteChannel : Channel<Route>
    {
        public RouteChannel(string guid, ConnectionScope scope, Route owner) : base(guid, scope, owner)
        {
        }

        public Task AbortAsync(string errorCode)
            => Scope.SendMessageToServer(
                Guid,
                "abort",
                errorCode);

        public Task FulfillAsync(NormalizedFulfillResponse response)
            => Scope.SendMessageToServer(
                Guid,
                "fulfill",
                response);

        public Task ContinueAsync(RouteContinueOverrides overrides = null)
            => Scope.SendMessageToServer(
                Guid,
                "continue",
                (object)overrides ?? new { });
    }
}
