using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;

namespace PlaywrightSharp.Helpers
{
    internal static class BrowserHelper
    {
        public static async Task<ITransport> CreateTransportAsync(ConnectOptions options)
        {
            if (!string.IsNullOrEmpty(options.BrowserWSEndpoint) && options.Transport != null)
            {
                throw new ArgumentException("Exactly one of BrowserWSEndpoint or Transport must be passed to connect");
            }

            ITransport transport = null;

            if (options.Transport != null)
            {
                transport = options.Transport;
            }
            else if (!string.IsNullOrEmpty(options.BrowserWSEndpoint))
            {
                transport = await WebsocketTransport.CreateAsync(options.BrowserWSEndpoint).ConfigureAwait(false);
            }

            return SlowMoTransport.Wrap(transport, options.SlowMo);
        }
    }
}
