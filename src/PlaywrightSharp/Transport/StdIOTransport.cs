using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class StdIOTransport : IConnectionTransport, IDisposable
    {
        private readonly Process _process;
        private readonly CancellationTokenSource _readerCancellationSource = new CancellationTokenSource();

        internal StdIOTransport(Process process, TransportTaskScheduler scheduler = null)
        {
            _process = process;
            scheduler ??= ScheduleTransportTask;

            scheduler(GetResponse, _readerCancellationSource.Token);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~StdIOTransport() => Dispose(false);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task SendAsync(string message)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message.ToCharArray());
            int len = bytes.Length;
            byte[] ll = new byte[4];
            ll[0] = (byte)(len & 0xFF);
            ll[1] = (byte)((len >> 8) & 0xFF);
            ll[2] = (byte)((len >> 16) & 0xFF);
            ll[3] = (byte)((len >> 24) & 0xFF);

            Console.WriteLine("Length");
            Console.WriteLine(len);
            await _process.StandardInput.BaseStream.WriteAsync(ll, 0, 4).ConfigureAwait(false);
            await _process.StandardInput.BaseStream.WriteAsync(bytes, 0, len).ConfigureAwait(false);
            await _process.StandardInput.BaseStream.FlushAsync().ConfigureAwait(false);
        }

        private static void ScheduleTransportTask(Action action, CancellationToken cancellationToken)
            => Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

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

        private void GetResponse()
        {
            var stream = _process.StandardOutput;

            while (!_process.HasExited && stream.Peek() > 0)
            {
                byte[] buffer = new byte[4];
                int read4 = stream.BaseStream.Read(buffer, 0, 4);
                if (read4 == 0)
                {
                    break;
                }

                int len = buffer[0] + (buffer[1] << 8) + (buffer[2] << 16) + (buffer[3] << 24);
                buffer = new byte[len];
                int readLen = stream.BaseStream.Read(buffer, 0, len);
                if (len != readLen)
                {
                    break;
                }

                string result = System.Text.Encoding.UTF8.GetString(buffer);
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(result));
            }
        }
    }
}
