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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport;

internal class StdIOTransport : IDisposable
{
    private const int DefaultBufferSize = 1024;  // Byte buffer size
    internal const int MaxMessageSize = 256 * 1024 * 1024;
    private readonly Process _process;
    private readonly CancellationTokenSource _readerCancellationSource = new();
    private readonly Task _getResponseTask;
    private readonly List<byte> _data = new();
    private int _consumed;
    private int? _currentMessageSize;

    internal StdIOTransport()
    {
        _process = GetProcess("run-driver");
        StartProcessWithUTF8IOEncoding(_process);
        _process.Exited += OnProcessExited;
        _process.ErrorDataReceived += OnProcessErrorDataReceived;
        _process.BeginErrorReadLine();

        _getResponseTask = ScheduleTransportTaskAsync(GetResponseAsync, _readerCancellationSource.Token);
    }

    ~StdIOTransport() => Dispose(false);

    public event EventHandler<byte[]>? MessageReceived;

    public event EventHandler<Exception>? TransportClosed;

    public event EventHandler<string>? LogReceived;

    public bool IsClosed { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Close(Exception closeReason)
    {
        Debug.WriteLine(closeReason);
        if (!IsClosed)
        {
            IsClosed = true;
            TransportClosed?.Invoke(this, closeReason);
            _readerCancellationSource?.Cancel();
            try
            {
                _process.StandardInput.Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
            }

            WaitForExitOrKill(_process, 3000);
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
                if (BitConverter.IsLittleEndian)
                {
                    ll[0] = (byte)(len & 0xFF);
                    ll[1] = (byte)((len >> 8) & 0xFF);
                    ll[2] = (byte)((len >> 16) & 0xFF);
                    ll[3] = (byte)((len >> 24) & 0xFF);
                }
                else
                {
                    ll[0] = (byte)((len >> 24) & 0xFF);
                    ll[1] = (byte)((len >> 16) & 0xFF);
                    ll[2] = (byte)((len >> 8) & 0xFF);
                    ll[3] = (byte)(len & 0xFF);
                }

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

    private static Process GetProcess(string driverArgs)
    {
        var (executablePath, cliEntrypoint) = Driver.GetExecutablePath();
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(cliEntrypoint);
        startInfo.ArgumentList.Add(driverArgs);

        foreach (var pair in Driver.EnvironmentVariables)
        {
            startInfo.EnvironmentVariables[pair.Key] = pair.Value;
        }
        return new()
        {
            StartInfo = startInfo,
        };
    }

    internal static bool WaitForExitOrKill(Process process, int timeoutMs)
    {
        try
        {
            if (process.HasExited || process.WaitForExit(timeoutMs))
            {
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }

        try
        {
            return process.WaitForExit(timeoutMs);
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    /// <summary>
    /// Starts the given process with UTF8-encoding for input and output without
    /// BOM which breaks driver transport otherwise.
    /// This function backports https://github.com/dotnet/runtime/issues/22051 to .NET Framework.
    /// See also https://github.com/microsoft/playwright-dotnet/issues/2517.
    /// See also https://stackoverflow.com/a/37258056/6512681.
    /// Can be removed after netstandard2.1+ where ProcessStartInfo.StandardInputEncoding is available.
    /// </summary>
    /// <param name="process">The process to start.</param>
    private static void StartProcessWithUTF8IOEncoding(Process process)
    {
        var encoding = new UTF8Encoding(false);
        var originalInputEncoding = Console.InputEncoding;
        var originalOutputEncoding = Console.OutputEncoding;

        var hasConsole = true;
        try
        {
            var height = Console.WindowHeight;
        }
        catch
        {
            hasConsole = false;
        }

        if (hasConsole)
        {
            Console.InputEncoding = encoding;
            Console.OutputEncoding = encoding;
        }

        try
        {
            process.Start();
        }
        finally
        {
            if (hasConsole)
            {
                try
                {
                    // Restore the original encodings
                    Console.InputEncoding = originalInputEncoding;
                    Console.OutputEncoding = originalOutputEncoding;
                }
                catch (System.IO.IOException)
                {
                    // It can fail under some conditions:
                    // https://github.com/microsoft/playwright-dotnet/issues/2888
                }
            }
        }
    }

    internal static Task ScheduleTransportTaskAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
        => Task.Factory.StartNew(() => func(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

    internal static int DecodeMessageSize(IReadOnlyList<byte> data, int offset)
    {
        // The protocol always uses little-endian for the length prefix.
        // On big-endian systems, swap the byte order.
        int messageSize;
        if (BitConverter.IsLittleEndian)
        {
            messageSize = data[offset + 0] + (data[offset + 1] << 8) + (data[offset + 2] << 16) + (data[offset + 3] << 24);
        }
        else
        {
            messageSize = (data[offset + 0] << 24) + (data[offset + 1] << 16) + (data[offset + 2] << 8) + data[offset + 3];
        }
        if (messageSize <= 0 || messageSize > MaxMessageSize)
        {
            throw new PlaywrightException($"Invalid driver message size: {messageSize}");
        }

        return messageSize;
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (_process != null)
        {
            _process.Exited -= OnProcessExited;
            _process.ErrorDataReceived -= OnProcessErrorDataReceived;
            _process.Dispose();
        }
        _readerCancellationSource?.Dispose();
        _getResponseTask?.Dispose();
    }

    private void OnProcessExited(object? sender, EventArgs e)
        => Close(new TargetClosedException("Process exited"));

    private void OnProcessErrorDataReceived(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            LogReceived?.Invoke(this, e.Data);
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
                if (read == 0)
                {
                    Close(new TargetClosedException("Driver connection closed"));
                    break;
                }

                if (!token.IsCancellationRequested)
                {
                    _data.AddRange(new ArraySegment<byte>(buffer, 0, read));

                    ProcessStream(token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            Close(ex);
        }
    }

    private void ProcessStream(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_currentMessageSize == null)
                {
                    if (_data.Count < _consumed + 4)
                    {
                        break;
                    }

                    _currentMessageSize = DecodeMessageSize(_data, _consumed);
                    _consumed += 4;
                }

                if (_data.Count - _consumed < _currentMessageSize)
                {
                    break;
                }

                byte[] result = new byte[_currentMessageSize.Value];
                _data.CopyTo(_consumed, result, 0, result.Length);
                _consumed += result.Length;
                _currentMessageSize = null;
                MessageReceived?.Invoke(this, result);

                // Compact buffer when more than half is consumed to avoid unbounded growth.
                if (_consumed > _data.Count / 2)
                {
                    _data.RemoveRange(0, _consumed);
                    _consumed = 0;
                }
            }
        }
        finally
        {
            if (_consumed > 0)
            {
                _data.RemoveRange(0, _consumed);
                _consumed = 0;
            }
        }
    }
}
