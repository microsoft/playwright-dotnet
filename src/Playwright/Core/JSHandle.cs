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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class JSHandle : ChannelOwner, IJSHandle
{
    internal JSHandle(ChannelOwner parent, string guid, JSHandleInitializer initializer) : base(parent, guid)
    {
        Preview = initializer.Preview;
    }

    protected string Preview { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IElementHandle AsElement() => this as IElementHandle;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<JsonElement?> EvaluateAsync(string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await SendMessageToServerAsync<JsonElement?>(
            "evaluateExpression",
            new Dictionary<string, object>
            {
                ["expression"] = expression,
                ["arg"] = ScriptsHelper.SerializedArgument(arg),
            }).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null)
        => await SendMessageToServerAsync<JSHandle>(
            "evaluateExpressionHandle",
            new Dictionary<string, object>
            {
                ["expression"] = expression,
                ["arg"] = ScriptsHelper.SerializedArgument(arg),
            }).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> EvaluateAsync<T>(string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await SendMessageToServerAsync<JsonElement?>(
            "evaluateExpression",
            new Dictionary<string, object>
            {
                ["expression"] = expression,
                ["arg"] = ScriptsHelper.SerializedArgument(arg),
            }).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> JsonValueAsync<T>() => ScriptsHelper.ParseEvaluateResult<T>(await SendMessageToServerAsync<JsonElement>("jsonValue").ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IJSHandle> GetPropertyAsync(string propertyName) => await SendMessageToServerAsync<JSHandle>(
            "getProperty",
            new Dictionary<string, object>
            {
                ["name"] = propertyName,
            }).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<Dictionary<string, IJSHandle>> GetPropertiesAsync()
    {
        var result = new Dictionary<string, IJSHandle>();
        var channelResult = (await SendMessageToServerAsync("getPropertyList").ConfigureAwait(false))?
            .GetProperty("properties").ToObject<List<JSElementProperty>>(_connection.DefaultJsonSerializerOptions);

        foreach (var kv in channelResult)
        {
            result[kv.Name] = kv.Value;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask DisposeAsync() => await SendMessageToServerAsync("dispose").ConfigureAwait(false);

    public override string ToString() => Preview;
}

internal class JSElementProperty
{
    public string Name { get; set; }

    public JSHandle Value { get; set; }
}
