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

namespace Microsoft.Playwright.Core
{
    internal partial class Tracing : ITracing
    {
        private readonly BrowserContext _context;

        public Tracing(BrowserContext context)
        {
            _context = context;
        }

        public async Task StartAsync(TracingStartOptions options = default)
        {
            await _context.Channel.TracingStartAsync(
                        name: options?.Name,
                        title: options?.Title,
                        screenshots: options?.Screenshots,
                        snapshots: options?.Snapshots,
                        sources: options?.Sources).ConfigureAwait(false);
            await _context.Channel.StartChunkAsync(options?.Title).ConfigureAwait(false);
        }

        public Task StartChunkAsync() => StartChunkAsync();

        public Task StartChunkAsync(TracingStartChunkOptions options) => _context.Channel.StartChunkAsync(title: options?.Title);

        public async Task StopChunkAsync(TracingStopChunkOptions options = null)
        {
            await DoStopChunkAsync(filePath: options.Path).ConfigureAwait(false);
        }

        public async Task StopAsync(TracingStopOptions options = default)
        {
            await StopChunkAsync(new() { Path = options?.Path }).ConfigureAwait(false);
            await _context.Channel.TracingStopAsync().ConfigureAwait(false);
        }

        private async Task DoStopChunkAsync(string filePath)
        {
            bool isLocal = _context.Channel.Connection.IsRemote;

            var mode = "doNotSave";
            if (!string.IsNullOrEmpty(filePath))
            {
                if (isLocal)
                    mode = "compressTraceAndSources";
                else
                    mode = "compressTrace";
            }

            var (artifact, sourceEntries) = await _context.Channel.StopChunkAsync(mode).ConfigureAwait(false);

            // Not interested in artifacts.
            if (string.IsNullOrEmpty(filePath))
                throw new PlaywrightException("Specified path was invalid or empty. Trace could not be saved.");

            // The artifact may be missing if the browser closed while stopping tracing.
            if (artifact == null)
                return;

            // Save trace to the final local file.
            await artifact.SaveAsAsync(filePath).ConfigureAwait(false);
            await artifact.DeleteAsync().ConfigureAwait(false);

            // Add local sources to the remote trace if necessary.
            if (sourceEntries.Count > 0)
                await _context.LocalUtils.ZipAsync(filePath, sourceEntries).ConfigureAwait(false);
        }
    }
}
