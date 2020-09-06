using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly DownloadInitializer _initializer;

        internal Download(ConnectionScope scope, string guid, DownloadInitializer initializer)
        {
            _scope = scope;
            _channel = new DownloadChannel(guid, scope, this);
            _initializer = initializer;
        }

        /// <summary>
        /// Returns downloaded url.
        /// </summary>
        public string Url => _initializer.Url;

        /// <summary>
        /// Returns suggested filename for this download.
        /// It is typically computed by the browser from the Content-Disposition response header or the download attribute. See the spec on whatwg.
        /// Different browsers can use different logic for computing it.
        /// </summary>
        public string SuggestedFilename => _initializer.SuggestedFilename;

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
        public Task<string> GetPathAsync() => _channel.GetPathAsync();

        /// <summary>
        /// Returns download error if any.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when failure is resolved, yielding the faulire.</returns>
        public Task<string> GetFailureAsync() => _channel.GetFailureAsync();

        /// <summary>
        /// Deletes the downloaded file.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the file is removed.</returns>
        public Task DeleteAsync() => _channel.DeleteAsync();

        /// <summary>
        /// Returns readable stream for current download or null if download failed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the stream is created, yielding the stream.</returns>
        public async Task<Stream> CreateReadStreamAsync()
        {
            string fileName = await GetPathAsync().ConfigureAwait(false);
            return string.IsNullOrEmpty(fileName) ? null : new FileStream(fileName, FileMode.Open);
        }
    }
}
