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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class WorkerChannel : Channel<Worker>
    {
        public WorkerChannel(string guid, Connection connection, Worker owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Closed;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(string script, object arg)
            => Connection.SendMessageToServerAsync<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["arg"] = arg,
                });

        internal Task<JsonElement?> EvaluateExpressionAsync(
            string script,
            object arg)
        {
            return Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["arg"] = arg,
                });
        }
    }
}
