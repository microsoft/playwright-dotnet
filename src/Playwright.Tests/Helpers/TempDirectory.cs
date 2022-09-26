/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using PathHelper = System.IO.Path;

namespace Microsoft.Playwright.Tests;

/// <summary>
/// Represents a directory that is deleted on disposal.
/// </summary>
internal class TempDirectory : IDisposable
{
    private Task _deleteTask;

    public TempDirectory() : this(PathHelper.Combine(Directory.GetCurrentDirectory(), ".temp", Guid.NewGuid().ToString()))
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
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
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
