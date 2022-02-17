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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport
{
    internal class StdIOTransport : IDisposable
    {
        private const int DefaultBufferSize = 32768;
        private readonly Process _process;
        private readonly CancellationTokenSource _readerCancellationSource = new();

        internal StdIOTransport()
        {
            _process = GetProcess();
            _process.StartInfo.Arguments = "run-driver";
            _process.Start();
            _process.Exited += (_, _) => Close("Process exited");
            _process.ErrorDataReceived += (_, error) =>
            {
                if (error.Data != null)
                    LogReceived?.Invoke(this, error.Data);
            };
            _process.BeginErrorReadLine();

            ScheduleTransportTask(OnMessageAsync, _readerCancellationSource.Token);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~StdIOTransport() => Dispose(false);

        public event EventHandler<byte[]> MessageReceived;

        public event EventHandler<string> TransportClosed;

        public event EventHandler<string> LogReceived;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Close(string closeReason)
        {
            if (!_readerCancellationSource.IsCancellationRequested)
            {
                _readerCancellationSource?.Cancel();
                _process.StandardInput.Close();
                _process.WaitForExit();
                TransportClosed.Invoke(this, closeReason);
            }
        }

        public async Task SendAsync(byte[] message)
        {
            if (!_readerCancellationSource.IsCancellationRequested)
            {
                byte[] lengthPrefix = BitConverter.GetBytes(message.Length);

#if NETSTANDARD
#pragma warning disable CA1835 // We can't use ReadOnlyMemory on netstandard
                await _process.StandardInput.BaseStream.WriteAsync(lengthPrefix, 0, 4, _readerCancellationSource.Token).ConfigureAwait(false);
                await _process.StandardInput.BaseStream.WriteAsync(message, 0, message.Length, _readerCancellationSource.Token).ConfigureAwait(false);
#pragma warning restore CA1835
#else
                await _process.StandardInput.BaseStream.WriteAsync(new(lengthPrefix, 0, 4), _readerCancellationSource.Token).ConfigureAwait(false);
                await _process.StandardInput.BaseStream.WriteAsync(new(message, 0, message.Length), _readerCancellationSource.Token).ConfigureAwait(false);
#endif
                await _process.StandardInput.BaseStream.FlushAsync(_readerCancellationSource.Token).ConfigureAwait(false);
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

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _readerCancellationSource?.Dispose();
            _process?.Dispose();
        }

        private async Task OnMessageAsync(CancellationToken token)
        {
            var stream = _process.StandardOutput.BaseStream;
            byte[] lengthBuffer = new byte[4];

            while (!token.IsCancellationRequested && !_process.HasExited)
            {
                int lengthBufferPos = 0;
                while (!token.IsCancellationRequested && lengthBufferPos < lengthBuffer.Length)
                {
#if NETSTANDARD
#pragma warning disable CA1835 // We can't use ReadOnlyMemory on netstandard
                    lengthBufferPos += await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length - lengthBufferPos, token).ConfigureAwait(false);
#pragma warning restore CA1835
#else
                    lengthBufferPos += await stream.ReadAsync(new(lengthBuffer, 0, lengthBuffer.Length - lengthBufferPos), token).ConfigureAwait(false);
#endif
                }

                int length = BitConverter.ToInt32(lengthBuffer, 0);
                int pos = 0;
                byte[] buffer = new byte[length];

                while (!token.IsCancellationRequested && pos < length)
                {
                    int toRead = Math.Min(length - pos, DefaultBufferSize);
#if NETSTANDARD
#pragma warning disable CA1835 // We can't use ReadOnlyMemory on netstandard
                    pos += await stream.ReadAsync(buffer, pos, toRead, token).ConfigureAwait(false);
#pragma warning restore CA1835
#else
                    pos += await stream.ReadAsync(new(buffer, pos, toRead), token).ConfigureAwait(false);
#endif
                }
                MessageReceived?.Invoke(this, buffer);
            }
        }
    }
}
