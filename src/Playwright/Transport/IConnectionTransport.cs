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
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport
{
    /// <summary>
    /// Transport interface.
    /// </summary>
    internal interface IConnectionTransport
    {
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when a log message is received.
        /// </summary>
        event EventHandler<LogReceivedEventArgs> LogReceived;

        /// <summary>
        /// Occurs when the transport is closed.
        /// </summary>
        event EventHandler<TransportClosedEventArgs> TransportClosed;

        /// <summary>
        /// Sends a message using the transport.
        /// </summary>
        /// <returns>The task.</returns>
        /// <param name="message">Message to send.</param>
        Task SendAsync(string message);

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="closeReason">Close reason.</param>
        void Close(string closeReason);
    }
}
