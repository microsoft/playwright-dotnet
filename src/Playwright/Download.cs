using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Download objects are dispatched by page via the <see cref="IPage.Download"/> event.
    /// All the downloaded files belonging to the browser context are deleted when the browser context is closed.All downloaded files are deleted when the browser closes.
    /// Download event is emitted once the download starts.
    /// </summary>
    public class Download : ChannelOwnerBase, IChannelOwner<Download>, IDownload
    {
        private readonly Connection _connection;
        private readonly DownloadChannel _channel;
        private readonly DownloadInitializer _initializer;

        internal Download(IChannelOwner parent, string guid, DownloadInitializer initializer) : base(parent, guid)
        {
            _connection = parent.Connection;
            _channel = new DownloadChannel(guid, parent.Connection, this);
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
        Connection IChannelOwner.Connection => _connection;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Download> IChannelOwner<Download>.Channel => _channel;

        /// <summary>
        /// Returns path to the downloaded file in case of successful download.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the download file path is resolved, yielding the path.</returns>
        public Task<string> PathAsync() => _channel.PathAsync();

        /// <summary>
        /// Returns download error if any.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when failure is resolved, yielding the faulire.</returns>
        public Task<string> FailureAsync() => _channel.GetFailureAsync();

        /// <summary>
        /// Deletes the downloaded file.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the file is removed.</returns>
        public Task DeleteAsync() => _channel.DeleteAsync();

        /// <summary>
        /// Saves the download to a user-specified path.
        /// </summary>
        /// <param name="path">Path where the download should be saved.</param>
        /// <returns>A <see cref="Task"/> that completes when the file is saved.</returns>
        public Task SaveAsAsync(string path) => _channel.SaveAsAsync(path);

        /// <summary>
        /// Returns readable stream for current download or null if download failed.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the stream is created, yielding the stream.</returns>
        public async Task<Stream> CreateReadStreamAsync()
        {
            string fileName = await PathAsync().ConfigureAwait(false);
            return string.IsNullOrEmpty(fileName) ? null : new FileStream(fileName, FileMode.Open);
        }
    }
}
