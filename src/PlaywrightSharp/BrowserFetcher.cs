using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers.Linux;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserFetcher"/>
    public sealed class BrowserFetcher : IBrowserFetcher, IDisposable
    {
        private readonly string _downloadsFolder;
        private readonly Platform _platform;
        private readonly string _preferredRevision;
        private readonly Func<Platform, string, BrowserFetcherConfig> _params;
        private readonly WebClient _webClient = new WebClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserFetcher"/> class.
        /// </summary>
        /// <param name="downloadsFolder">Folder to use.</param>
        /// <param name="platform">Current platform (some browsers are more or less specific).</param>
        /// <param name="preferredRevision">Revision to download.</param>
        /// <param name="paramsGetter">Function use to return argumens based on a platform and a revision.</param>
        public BrowserFetcher(string downloadsFolder, Platform platform, string preferredRevision, Func<Platform, string, BrowserFetcherConfig> paramsGetter)
        {
            _downloadsFolder = downloadsFolder;
            _platform = platform;
            _preferredRevision = preferredRevision;
            _params = paramsGetter;
        }

        /// <inheritdoc cref="IDisposable"/>
        ~BrowserFetcher() => Dispose(false);

        /// <inheritdoc cref="IBrowserFetcher"/>
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        /// <inheritdoc cref="IBrowserFetcher"/>
        public async Task<bool> CanDownloadAsync(string revision)
        {
            try
            {
                string url = _params(_platform, revision).DownloadURL;

                var client = WebRequest.Create(url);
                client.Proxy = _webClient.Proxy;
                client.Method = "HEAD";
                var response = await client.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse;
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException)
            {
                return false;
            }
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public async Task<RevisionInfo> DownloadAsync(string revision = null)
        {
            revision ??= _preferredRevision;

            string url = _params(_platform, revision).DownloadURL;
            string zipPath = Path.Combine(_downloadsFolder, $"download-{_platform.ToString()}-{revision}.zip");
            string folderPath = GetFolderPath(revision);

            if (new DirectoryInfo(folderPath).Exists)
            {
                return GetRevisionInfo(revision);
            }

            var downloadFolder = new DirectoryInfo(_downloadsFolder);
            if (!downloadFolder.Exists)
            {
                downloadFolder.Create();
            }

            if (DownloadProgressChanged != null)
            {
                _webClient.DownloadProgressChanged += DownloadProgressChanged;
            }

            await _webClient.DownloadFileTaskAsync(new Uri(url), zipPath).ConfigureAwait(false);

            if (_platform == Platform.MacOS)
            {
                // ZipFile and many others unzip libraries have issues extracting .app files
                // Until we have a clear solution we'll call the native unzip tool
                // https://github.com/dotnet/corefx/issues/15516
                NativeExtractToDirectory(zipPath, folderPath);
            }
            else
            {
                ZipFile.ExtractToDirectory(zipPath, folderPath);
            }

            new FileInfo(zipPath).Delete();

            var revisionInfo = GetRevisionInfo(revision);

            if (revisionInfo != null && GetCurrentPlatform() == Platform.Linux)
            {
                if (LinuxSysCall.Chmod(revisionInfo.ExecutablePath, LinuxSysCall.ExecutableFilePermissions) != 0)
                {
                    throw new PlaywrightSharpException("Unable to chmod the BrowserApp");
                }
            }

            return revisionInfo;
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public IEnumerable<string> GetLocalRevisions()
        {
            var directoryInfo = new DirectoryInfo(_downloadsFolder);

            if (directoryInfo.Exists)
            {
                return directoryInfo.GetDirectories()
                    .Select(d => ParseFolderPath(d.Name))
                    .Where(v => v.platform == _platform)
                    .Select(v => v.revision);
            }

            return Array.Empty<string>();
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public RevisionInfo GetRevisionInfo(string revision)
        {
            var paramsFunctions = _params(_platform, revision);

            var result = new RevisionInfo
            {
                FolderPath = GetFolderPath(revision),
                Url = paramsFunctions.DownloadURL,
                Revision = revision,
                Platform = _platform,
            };
            result.ExecutablePath = Path.Combine(result.FolderPath, paramsFunctions.ExecutablePath);
            result.Local = new DirectoryInfo(result.FolderPath).Exists;

            return result;
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public void Remove(string revision)
        {
            var directory = new DirectoryInfo(GetFolderPath(revision));
            if (directory.Exists)
            {
                directory.Delete(true);
            }
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webClient?.Dispose();
            }
        }

        private void NativeExtractToDirectory(string zipPath, string folderPath)
        {
            using var process = new Process();

            process.StartInfo.FileName = "unzip";
            process.StartInfo.Arguments = $"\"{zipPath}\" -d \"{folderPath}\"";
            process.Start();
            process.WaitForExit();
        }

        private (Platform platform, string revision) ParseFolderPath(string folderName)
        {
            string[] splits = folderName.Split('-');

            if (splits.Length != 2)
            {
                return (Platform.Unknown, "0");
            }

            if (!Enum.TryParse<Platform>(splits[0], out var platform))
            {
                platform = Platform.Unknown;
            }

            return (platform, splits[1]);
        }

        private Platform GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.MacOS;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Platform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimeInformation.OSArchitecture == Architecture.X64 ? Platform.Win64 : Platform.Win32;
            }

            return Platform.Unknown;
        }

        private string GetFolderPath(string revision)
            => Path.Combine(_downloadsFolder, $"{_platform.ToString()}-{revision}");
    }
}
