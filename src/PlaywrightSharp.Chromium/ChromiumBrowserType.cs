using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserType"/>
    public class ChromiumBrowserType : IBrowserType
    {
        /// <inheritdoc cref="IBrowserType"/>
        public ChromiumBrowserType()
        {
        }

        /// <inheritdoc cref="IBrowserType"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => null;

        /// <inheritdoc cref="IBrowserType"/>
        public Task<IBrowser> ConnectAsync(ConnectOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public string[] GetDefaultArgs(BrowserArgOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}
