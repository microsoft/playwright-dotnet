/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport
{
    internal class StdIOTransport : IConnectionTransport, IDisposable
    {
        private const int DefaultBufferSize = 1024;  // Byte buffer size
        private readonly Process _process;
        private readonly CancellationTokenSource _readerCancellationSource = new();
        private readonly List<byte> _data = new();
        private int? _currentMessageSize;

        internal StdIOTransport()
        {
            _process = GetProcess();
            _process.StartInfo.Arguments = "run-driver";
            _process.Start();
            _process.Exited += (_, _) => Close("Process exited");
            _process.ErrorDataReceived += (_, error) =>
            {
                if (error.Data != null)
                {
                    LogReceived?.Invoke(this, error.Data);
                }
            };
            _process.BeginErrorReadLine();

            ScheduleTransportTask(GetResponseAsync, _readerCancellationSource.Token);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~StdIOTransport() => Dispose(false);

        public event EventHandler<byte[]> MessageReceived;

        public event EventHandler<string> TransportClosed;

        public event EventHandler<string> LogReceived;

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
                TransportClosed.Invoke(this, closeReason);
                _readerCancellationSource?.Cancel();
                _process.StandardInput.Close();
                _process.WaitForExit();
            }
        }

        public async Task SendAsync(byte[] message)
        {
            try
            {
                if (!_readerCancellationSource.IsCancellationRequested)
                {
                    int len = message.Length;
                    byte[] ll = new byte[4];
                    ll[0] = (byte)(len & 0xFF);
                    ll[1] = (byte)((len >> 8) & 0xFF);
                    ll[2] = (byte)((len >> 16) & 0xFF);
                    ll[3] = (byte)((len >> 24) & 0xFF);

#if NETSTANDARD
#pragma warning disable CA1835 // We can't use ReadOnlyMemory on netstandard
                    await _process.StandardInput.BaseStream.WriteAsync(ll, 0, 4, _readerCancellationSource.Token).ConfigureAwait(false);
                    await _process.StandardInput.BaseStream.WriteAsync(message, 0, len, _readerCancellationSource.Token).ConfigureAwait(false);
#pragma warning restore CA1835
#else
                    await _process.StandardInput.BaseStream.WriteAsync(new(ll, 0, 4), _readerCancellationSource.Token).ConfigureAwait(false);
                    await _process.StandardInput.BaseStream.WriteAsync(new(message, 0, len), _readerCancellationSource.Token).ConfigureAwait(false);
#endif
                    await _process.StandardInput.BaseStream.FlushAsync(_readerCancellationSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private static Process GetProcess()
        {
            var startInfo = new ProcessStartInfo(Driver.GetExecutablePath())
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var pair in Driver.GetEnvironmentVariables())
            {
                startInfo.EnvironmentVariables[pair.Key] = pair.Value;
            }
            return new()
            {
                StartInfo = startInfo,
            };
        }

        private static void ScheduleTransportTask(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(() => func(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        private void Close(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Close(ex.ToString());
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _readerCancellationSource?.Dispose();
            _process?.Dispose();
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
                    int read = await stream.BaseStream.ReadAsync(new(buffer, 0, DefaultBufferSize), token).ConfigureAwait(false);
#endif
                    if (!token.IsCancellationRequested)
                    {
                        _data.AddRange(buffer.AsSpan(0, read).ToArray());

                        ProcessStream(token);
                    }
                }
            }
            catch (Exception ex)
            {
                Close(ex);
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

                byte[] result = _data.GetRange(0, _currentMessageSize.Value).ToArray();
                _data.RemoveRange(0, _currentMessageSize.Value);
                _currentMessageSize = null;
                MessageReceived?.Invoke(this, result);
            }
        }
    }
}
