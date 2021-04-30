using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Playwright.Transport
{
    internal class StdIOTransport : IConnectionTransport, IDisposable
    {
        private const int DefaultBufferSize = 1024;  // Byte buffer size
        private readonly Process _process;
        private readonly ILogger<StdIOTransport> _logger;
        private readonly CancellationTokenSource _readerCancellationSource = new();
        private readonly List<byte> _data = new();
        private int? _currentMessageSize;

        internal StdIOTransport(Process process, ILoggerFactory loggerFactory, TransportTaskScheduler scheduler = null)
        {
            _process = process;
            _logger = loggerFactory?.CreateLogger<StdIOTransport>();
            scheduler ??= ScheduleTransportTask;
            process.ErrorDataReceived += (_, e) => LogReceived?.Invoke(this, new LogReceivedEventArgs(e.Data));
            process.BeginErrorReadLine();

            scheduler(GetResponseAsync, _readerCancellationSource.Token);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~StdIOTransport() => Dispose(false);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<TransportClosedEventArgs> TransportClosed;

        public event EventHandler<LogReceivedEventArgs> LogReceived;

        public bool IsClosed { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Close(string closeReason)
        {
            if (!IsClosed)
            {
                IsClosed = true;
                TransportClosed?.Invoke(this, new TransportClosedEventArgs { CloseReason = closeReason });
                _readerCancellationSource.Cancel();
            }
        }

        public async Task SendAsync(string message)
        {
            try
            {
                if (!_readerCancellationSource.IsCancellationRequested)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message.ToCharArray());
                    int len = bytes.Length;
                    byte[] ll = new byte[4];
                    ll[0] = (byte)(len & 0xFF);
                    ll[1] = (byte)((len >> 8) & 0xFF);
                    ll[2] = (byte)((len >> 16) & 0xFF);
                    ll[3] = (byte)((len >> 24) & 0xFF);

#if NETSTANDARD
#pragma warning disable CA1835 // We can't use ReadOnlyMemory on netstandard
                    await _process.StandardInput.BaseStream.WriteAsync(ll, 0, 4, _readerCancellationSource.Token).ConfigureAwait(false);
                    await _process.StandardInput.BaseStream.WriteAsync(bytes, 0, len, _readerCancellationSource.Token).ConfigureAwait(false);
#pragma warning restore CA1835
#else
                    await _process.StandardInput.BaseStream.WriteAsync(new ReadOnlyMemory<byte>(ll, 0, 4), _readerCancellationSource.Token).ConfigureAwait(false);
                    await _process.StandardInput.BaseStream.WriteAsync(new ReadOnlyMemory<byte>(bytes, 0, len), _readerCancellationSource.Token).ConfigureAwait(false);
#endif
                    await _process.StandardInput.BaseStream.FlushAsync(_readerCancellationSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Transport Error");
                Close(ex.ToString());
            }
        }

        private static void ScheduleTransportTask(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(() => func(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_readerCancellationSource != null)
            {
                _readerCancellationSource.Cancel();
                _readerCancellationSource.Dispose();
            }
        }

        private async Task GetResponseAsync(CancellationToken token)
        {
            try
            {
                var stream = _process.StandardOutput;
                byte[] buffer = new byte[DefaultBufferSize];

                while (!token.IsCancellationRequested && !_process.HasExited)
                {
#if NETSTANDARD
#pragma warning disable CA1835 // We can't use ReadOnlyMemory on netstandard
                    int read = await stream.BaseStream.ReadAsync(buffer, 0, DefaultBufferSize, token).ConfigureAwait(false);
#pragma warning restore CA1835
#else
                    int read = await stream.BaseStream.ReadAsync(new Memory<byte>(buffer, 0, DefaultBufferSize), token).ConfigureAwait(false);
#endif
                    if (!token.IsCancellationRequested)
                    {
                        _data.AddRange(buffer.AsMemory().Slice(0, read).ToArray());

                        ProcessStream(token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Transport Error");
                Close(ex.ToString());
            }
        }

        private void ProcessStream(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_currentMessageSize == null && _data.Count < 4)
                {
                    break;
                }

                if (_currentMessageSize == null)
                {
                    _currentMessageSize = _data[0] + (_data[1] << 8) + (_data[2] << 16) + (_data[3] << 24);
                    _data.RemoveRange(0, 4);
                }

                if (_data.Count < _currentMessageSize)
                {
                    break;
                }

                string result = System.Text.Encoding.UTF8.GetString(_data.GetRange(0, _currentMessageSize.Value).ToArray());
                _data.RemoveRange(0, _currentMessageSize.Value);
                _currentMessageSize = null;
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(result));
            }
        }
    }
}
