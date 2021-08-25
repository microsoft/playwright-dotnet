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
*
*
* ------------------------------------------------------------------------------
* <auto-generated>
* This file is generated by generate_dotnet_channels.js, do not edit manually.
* </auto-generated>
*/

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

#nullable enable
namespace Microsoft.Playwright.Transport.Channels
{
    internal class WorkerChannelImpl : Channel<Worker>
    {
        public WorkerChannelImpl(string guid, Connection connection, Worker owner) : base(guid, connection, owner)
        {
        }

        // invoked via close
        internal event EventHandler? Close;

        internal virtual async Task<JsonElement> EvaluateExpressionAsync(string expression,
                bool? isFunction,
                object arg)
            => (await Connection.SendMessageToServerAsync<JsonElement>(
                Guid,
                "evaluateExpression",
                    new
                    {
                        expression = expression,
                        isFunction = isFunction,
                        arg = arg,
                    }
                )
                .ConfigureAwait(false)).GetProperty("value");
        internal virtual async Task<JSHandle> EvaluateExpressionHandleAsync(string expression,
                bool? isFunction,
                object arg)
            => (await Connection.SendMessageToServerAsync<JsonElement>(
                Guid,
                "evaluateExpressionHandle",
                    new
                    {
                        expression = expression,
                        isFunction = isFunction,
                        arg = arg,
                    }
                )
                .ConfigureAwait(false)).GetObject<JSHandle>("handle", Connection);

        protected void OnClose() => Close?.Invoke(this, new());
    }

    internal partial class WorkerChannel : WorkerChannelImpl
    {
        public WorkerChannel(string guid, Connection connection, Worker owner) : base(guid, connection, owner)
        {
        }
    }
}
#nullable disable
