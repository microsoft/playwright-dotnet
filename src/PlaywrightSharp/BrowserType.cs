using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc />
    public class BrowserType : IBrowserType
    {
        /// <summary>
        /// Browser type Chromium.
        /// </summary>
        public const string Chromium = "chromium";

        /// <summary>
        /// Browser type Firefox.
        /// </summary>
        public const string Firefox = "firefox";

        /// <summary>
        /// Browser type WebKit.
        /// </summary>
        public const string Webkit = "webkit";

        /// <inheritdoc />
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices { get; }

        /// <inheritdoc />
        public string ExecutablePath { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public string[] GetDefaultArgs(BrowserArgOptions options = null) => throw new System.NotImplementedException();

        /// <inheritdoc />
        public Task<IBrowser> ConnectAsync(ConnectOptions options = null) => throw new System.NotImplementedException();
    }
}
