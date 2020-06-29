using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PathHelper = System.IO.Path;

namespace PlaywrightSharp.Helpers
{
    /// <summary>
    /// Represents a directory that is deleted on disposal.
    /// </summary>
    internal class TempDirectory : IDisposable
    {
        private Task _deleteTask;

        public TempDirectory() : this(PathHelper.Combine(Directory.GetCurrentDirectory(), ".temp", PathHelper.GetRandomFileName()))
        {
        }

        private TempDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path must be specified", nameof(path));
            }

            Directory.CreateDirectory(path);
            Path = path;
        }

        ~TempDirectory()
        {
            Dispose(false);
        }

        public string Path { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public override string ToString() => Path;

        private static async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            const int minDelayInMs = 200;
            const int maxDelayInMs = 8000;

            int retryDelay = minDelayInMs;
            while (true)
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch (IOException)
                {
                    await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                    if (retryDelay < maxDelayInMs)
                    {
                        retryDelay = Math.Min(2 * retryDelay, maxDelayInMs);
                    }
                }
            }
        }

        private Task DeleteAsync(CancellationToken cancellationToken = default)
            => _deleteTask ??= DeleteAsync(Path, cancellationToken);

        private void Dispose(bool disposing)
        {
            if (_deleteTask == null && disposing)
            {
                _ = DeleteAsync();
            }
        }
    }
}
