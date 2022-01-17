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
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright
{
    [SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
    public static class Playwright
    {
        /// <summary>
        /// Launches Playwright.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var transport = new StdIOTransport();
#pragma warning restore CA2000
            var connection = new Connection();
            transport.MessageReceived += (_, message) => connection.Dispatch(JsonSerializer.Deserialize<PlaywrightServerMessage>(message, JsonExtensions.DefaultJsonSerializerOptions));
            transport.LogReceived += (_, log) => Console.Error.WriteLine(log);
            transport.TransportClosed += (_, reason) => connection.DoClose(reason);
            connection.OnMessage = (message) => transport.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, connectionDefaultJsonSerializerOptions));
            connection.Close += (_, e) => transport.Close(e);
            var playwright = await connection.InitializePlaywrightAsync().ConfigureAwait(false);
            playwright.Connection = connection;
            return playwright;
        }
    }
}
