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

using System.Net.Sockets;

namespace Microsoft.Playwright.Tests.Fixtures;

internal static class SafeAccept
{
    /// <summary>
    /// Accepts a connection with a blocking call on a thread of its own. On macOS the asynchronous accept
    /// of .NET throws on a thread-pool thread, where nothing can catch it, when the client resets the
    /// connection before the accept completes ("The supplied SocketAddress is an invalid size"): that ends
    /// the test process. The blocking call throws to the caller.
    /// </summary>
    public static Task<TcpClient> AcceptAsync(TcpListener listener) =>
        Task.Factory.StartNew(listener.AcceptTcpClient, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
}
