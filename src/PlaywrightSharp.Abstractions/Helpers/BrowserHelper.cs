using System;
using System.Threading.Tasks;
using PlaywrightSharp.Server;

namespace PlaywrightSharp.Helpers
{
    internal static class BrowserHelper
    {
        public static async Task<IConnectionTransport> CreateTransportAsync(ConnectOptions options)
        {
            if (!string.IsNullOrEmpty(options.WSEndpoint) && options.TransportFactory != null)
            {
                throw new ArgumentException("Exactly one of WSEndpoint or TransportFactory must be passed to connect");
            }

            IConnectionTransport transport = null;

            if (options.TransportFactory != null)
            {
                transport = await options.TransportFactory(new Uri(options.WSEndpoint), options).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(options.WSEndpoint))
            {
#pragma warning disable CA2000 // Call dispose, this is a false alarm.
                transport = await WebSocketTransport.CreateAsync(options).ConfigureAwait(false);
#pragma warning restore CA2000
            }

            return SlowMoTransport.Wrap(transport, options.SlowMo);
        }
    }
}
