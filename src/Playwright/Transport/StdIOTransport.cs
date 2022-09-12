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

namespace Microsoft.Playwright.Transport;

internal class StdIOTransport : IDisposable
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

    ~StdIOTransport() => Dispose(false);

    public event EventHandler<byte[]> MessageReceived;

    public event EventHandler<string> TransportClosed;

    public event EventHandler<string> LogReceived;

    public bool IsClosed { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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

                await _process.StandardInput.BaseStream.WriteAsync(ll, 0, 4, _readerCancellationSource.Token).ConfigureAwait(false);
                await _process.StandardInput.BaseStream.WriteAsync(message, 0, len, _readerCancellationSource.Token).ConfigureAwait(false);
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
                int read = await stream.BaseStream.ReadAsync(buffer, 0, DefaultBufferSize, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                {
                    _data.AddRange(new ArraySegment<byte>(buffer, 0, read));

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
        var offset = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_currentMessageSize == null)
                {
                    if (_data.Count < (uint)offset + 4)
                    {
                        break;
                    }

                    _currentMessageSize = _data[offset + 0] + (_data[offset + 1] << 8) + (_data[offset + 2] << 16) + (_data[offset + 3] << 24);
                    offset += 4;
                }

                if (_data.Count < (uint)offset + _currentMessageSize)
                {
                    break;
                }

                byte[] result = new byte[_currentMessageSize.Value];
                _data.CopyTo(offset, result, 0, result.Length);
                offset += result.Length;
                _currentMessageSize = null;
                MessageReceived?.Invoke(this, result);
            }
        }
        finally
        {
            _data.RemoveRange(0, offset);
        }
    }
}
