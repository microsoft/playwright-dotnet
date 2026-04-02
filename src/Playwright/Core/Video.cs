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

using System.Threading.Tasks;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Core;

internal class Video : IVideo
{
    private readonly bool _isRemote;
    private readonly Artifact? _artifact;

    public Video(Page page, Connection connection, Artifact? artifact = null)
    {
        _isRemote = connection.IsRemote;
        _artifact = artifact;
    }

    public async Task DeleteAsync()
    {
        if (_artifact != null)
        {
            await _artifact.DeleteAsync().ConfigureAwait(false);
        }
    }

    public Task<string> PathAsync()
    {
        if (_isRemote)
        {
            throw new PlaywrightException("Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy.");
        }
        if (_artifact == null)
        {
            throw new PlaywrightException("Video recording has not been started.");
        }
        return Task.FromResult(_artifact.AbsolutePath);
    }

    public async Task SaveAsAsync(string path)
    {
        if (_artifact == null)
        {
            throw new PlaywrightException("Video recording has not been started.");
        }
        await _artifact.SaveAsAsync(path).ConfigureAwait(false);
    }
}
