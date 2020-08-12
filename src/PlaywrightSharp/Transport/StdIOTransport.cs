using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class StdIOTransport : IConnectionTransport, IDisposable
    {
        private const int DefaultBufferSize = 1024;  // Byte buffer size
        private readonly Process _process;
        private readonly CancellationTokenSource _readerCancellationSource = new CancellationTokenSource();
        private readonly List<byte> _data = new List<byte>();
        private int? _currentMessageSize = null;

        internal StdIOTransport(Process process, TransportTaskScheduler scheduler = null)
        {
            _process = process;
            scheduler ??= ScheduleTransportTask;

            scheduler(GetResponseAsync, _readerCancellationSource.Token);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~StdIOTransport() => Dispose(false);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<TransportClosedEventArgs> TransportClosed;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Close(string closeReason)
        {
            TransportClosed?.Invoke(this, new TransportClosedEventArgs { CloseReason = closeReason });
            _readerCancellationSource.Cancel();
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

                    await _process.StandardInput.BaseStream.WriteAsync(ll, 0, 4, _readerCancellationSource.Token).ConfigureAwait(false);
                    await _process.StandardInput.BaseStream.WriteAsync(bytes, 0, len, _readerCancellationSource.Token).ConfigureAwait(false);
                    await _process.StandardInput.BaseStream.FlushAsync(_readerCancellationSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
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
                    int read = await stream.BaseStream.ReadAsync(buffer, 0, DefaultBufferSize, token).ConfigureAwait(false);

                    if (!token.IsCancellationRequested)
                    {
                        _data.AddRange(buffer.AsMemory().Slice(0, read).ToArray());

                        ProcessStream(token);
                    }
                }
            }
            catch (Exception ex)
            {
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
