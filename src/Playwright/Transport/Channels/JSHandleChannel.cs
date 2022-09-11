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

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels;

internal class JSHandleChannel : Channel<JSHandle>
{
    public JSHandleChannel(string guid, Connection connection, JSHandle owner) : base(guid, connection, owner)
    {
    }

    internal Task<JsonElement?> EvaluateExpressionAsync(string script, object arg)
        => Connection.SendMessageToServerAsync<JsonElement?>(
            Guid,
            "evaluateExpression",
            new Dictionary<string, object>
            {
                ["expression"] = script,
                ["arg"] = arg,
            });

    internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(string script, object arg)
        => Connection.SendMessageToServerAsync<JSHandleChannel>(
            Guid,
            "evaluateExpressionHandle",
            new Dictionary<string, object>
            {
                ["expression"] = script,
                ["arg"] = arg,
            });

    internal Task<JsonElement> JsonValueAsync() => Connection.SendMessageToServerAsync<JsonElement>(Guid, "jsonValue", null);

    internal Task DisposeAsync() => Connection.SendMessageToServerAsync(Guid, "dispose", null);

    internal Task<JSHandleChannel> GetPropertyAsync(string propertyName)
        => Connection.SendMessageToServerAsync<JSHandleChannel>(
            Guid,
            "getProperty",
            new Dictionary<string, object>
            {
                ["name"] = propertyName,
            });

    internal async Task<List<JSElementProperty>> GetPropertiesAsync()
        => (await Connection.SendMessageToServerAsync(Guid, "getPropertyList", null).ConfigureAwait(false))?
            .GetProperty("properties").ToObject<List<JSElementProperty>>(Connection.DefaultJsonSerializerOptions);

    internal class JSElementProperty
    {
        public string Name { get; set; }

        public JSHandleChannel Value { get; set; }
    }
}
