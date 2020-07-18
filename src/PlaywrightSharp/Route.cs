using System;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <summary>
    /// Whenever a network route is set up with <see cref="IPage.RouteAsync(string, Action{Route, IRequest})"/> or <see cref="IBrowserContext.RouteAsync(string, Action{Route, IRequest})"/> the Route object allows to handle the route.
    /// </summary>
    public class Route : IChannelOwner<Route>
    {
        private readonly ConnectionScope _scope;
        private readonly RouteChannel _channel;

        internal Route(ConnectionScope scope, string guid, RouteInitializer initializer)
        {
            _scope = scope;
            _channel = new RouteChannel(guid, scope, this);
        }

        /// <summary>
        /// A request to be routed.
        /// </summary>
        public IRequest Request { get; internal set; }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<Route> IChannelOwner<Route>.Channel => _channel;

        /// <summary>
        /// Fulfills route's request with given response.
        /// </summary>
        /// <param name="response">Response that will fulfill this route's request.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task FulfillAsync(RouteFilfillResponse response)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Continues route's request with optional overrides.
        /// </summary>
        /// <param name="overrides">Optional request overrides.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        public Task ContinueAsync(RouteContinueOverrides overrides = null)
        {
            throw new NotImplementedException();
        }
    }
}
