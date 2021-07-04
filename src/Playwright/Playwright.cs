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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright
{
    [SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
    public static class Playwright
    {
        private static Process _playwrightServerProcess;

        /// <summary>
        /// Launches Playwright.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter((f, _) => f == "PlaywrightSharp.Playwright");
            });
            _playwrightServerProcess = GetProcess();
            _playwrightServerProcess.StartInfo.Arguments = "run-driver";
            _playwrightServerProcess.Start();
            _playwrightServerProcess.Exited += (_, _) => Close();

            var transport = new StdIOTransport(_playwrightServerProcess, loggerFactory);
            var connection = new Connection(transport, loggerFactory);
            var playwright = await connection.WaitForObjectWithKnownNameAsync<PlaywrightImpl>("Playwright").ConfigureAwait(false);
            playwright.Connection = connection;
            return playwright;
        }

        private static Process GetProcess()
            => new()
            {
                StartInfo =
                {
                    FileName = Paths.GetExecutablePath(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

        private static void Close()
        {
            try
            {
                _playwrightServerProcess?.Kill();
                _playwrightServerProcess?.Dispose();
            }
            catch
            {
            }
        }
    }
}
