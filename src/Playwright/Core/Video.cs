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
    private readonly TaskCompletionSource<Artifact> _artifactTcs = new();
    private readonly bool _isRemote;

    public Video(Page page, Connection connection)
    {
        _isRemote = connection.IsRemote;
        page.Close += (_, _) => _artifactTcs.TrySetCanceled();
        page.Crash += (_, _) => _artifactTcs.TrySetCanceled();
    }

    public async Task DeleteAsync()
    {
        var artifact = await _artifactTcs.Task.ConfigureAwait(false);
        await artifact.DeleteAsync().ConfigureAwait(false);
    }

    public async Task<string> PathAsync()
    {
        if (_isRemote)
        {
            throw new PlaywrightException("Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy.");
        }
        var artifact = await _artifactTcs.Task.ConfigureAwait(false);
        return artifact.AbsolutePath;
    }

    public async Task SaveAsAsync(string path)
    {
        var artifact = await _artifactTcs.Task.ConfigureAwait(false);
        await artifact.SaveAsAsync(path).ConfigureAwait(false);
    }

    internal void ArtifactReady(Artifact artifact) => _artifactTcs.TrySetResult(artifact);
}
