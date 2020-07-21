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

        /// <summary>
        /// Fulfills route's request with given response.
        /// </summary>
        /// <param name="response">Response that will fulfill this route's request.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task FulfillAsync(RouteFilfillResponse response)
            => Scope.SendMessageToServer(
                Guid,
                "fulfill",
                response);

        /// <summary>
        /// Continues route's request with optional overrides.
        /// </summary>
        /// <param name="overrides">Optional request overrides.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task ContinueAsync(RouteContinueOverrides overrides = null)
            => Scope.SendMessageToServer(
                Guid,
                "continue",
                (object)overrides ?? new { });
    }
}
