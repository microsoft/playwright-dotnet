using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <summary>
    /// Download objects are dispatched by page via the <see cref="IPage.Download"/> event.
    /// All the downloaded files belonging to the browser context are deleted when the browser context is closed.All downloaded files are deleted when the browser closes.
    /// Download event is emitted once the download starts.
    /// </summary>
    public class Download : IChannelOwner<Download>
    {
        private readonly ConnectionScope _scope;
        private readonly DownloadChannel _channel;

        internal Download(ConnectionScope scope, string guid, DownloadInitializer initializer)
        {
            _scope = scope;
            _channel = new DownloadChannel(guid, scope, this);
        }

        /// <summary>
        /// Returns downloaded url.
        /// </summary>
        public string Url { get; internal set; }

        /// <summary>
        /// Returns suggested filename for this download.
        /// It is typically computed by the browser from the Content-Disposition response header or the download attribute. See the spec on whatwg.
        /// Different browsers can use different logic for computing it.
        /// </summary>
        public string SuggestedFilename { get; internal set; }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Download> IChannelOwner<Download>.Channel => _channel;

        /// <summary>
        /// Returns path to the downloaded file in case of successful download.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the download file path is resolved, yielding the path.</returns>
        public Task<string> GetPathAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns download error if any.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when faulired is resolved, yielding the faulire.</returns>
        internal Task<string> GetFailureAsync()
        {
            throw new NotImplementedException();
        }
    }
}
