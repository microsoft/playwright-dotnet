using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;

namespace PlaywrightSharp.Helpers
{
    internal static class BrowserHelper
    {
        public static async Task<IConnectionTransport> CreateTransportAsync(ConnectOptions options)
        {
            if (!string.IsNullOrEmpty(options.BrowserWSEndpoint) && options.TransportFactory != null)
            {
                throw new ArgumentException("Exactly one of BrowserWSEndpoint or TransportFactory must be passed to connect");
            }

            IConnectionTransport transport = null;

            if (options.TransportFactory != null)
            {
                transport = await options.TransportFactory(new Uri(options.BrowserWSEndpoint), options).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(options.BrowserWSEndpoint))
            {
                transport = await WebSocketTransport.CreateAsync(options).ConfigureAwait(false);
            }

            return SlowMoTransport.Wrap(transport, options.SlowMo);
        }
    }
}
