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
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    /// <summary>
    /// Download objects are dispatched by page via the <see cref="IPage.Download"/> event.
    /// All the downloaded files belonging to the browser context are deleted when the browser context is closed.All downloaded files are deleted when the browser closes.
    /// Download event is emitted once the download starts.
    /// </summary>
    internal class Download : IDownload
    {
        private readonly Artifact _artifact;

        internal Download(IPage page, string url, string suggestedFilename, Artifact artifact)
        {
            Page = page;
            Url = url;
            SuggestedFilename = suggestedFilename;
            _artifact = artifact;
        }

        public IPage Page { get; }

        public string Url { get; }

        public string SuggestedFilename { get; }

        public Task<string> PathAsync() => _artifact.PathAfterFinishedAsync();

        public Task<string> FailureAsync() => _artifact.FailureAsync();

        public Task DeleteAsync() => _artifact.DeleteAsync();

        public Task SaveAsAsync(string path) => _artifact.SaveAsAsync(path);

        public Task<Stream> CreateReadStreamAsync() => _artifact.CreateReadStreamAsync();
    }
}
