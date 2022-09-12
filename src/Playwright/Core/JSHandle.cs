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
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class JSHandle : ChannelOwnerBase, IChannelOwner<JSHandle>, IJSHandle
{
    private readonly JSHandleChannel _channel;

    internal JSHandle(IChannelOwner parent, string guid, JSHandleInitializer initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
        Preview = initializer.Preview;
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<JSHandle> IChannelOwner<JSHandle>.Channel => _channel;

    internal string Preview { get; set; }

    public IElementHandle AsElement() => this as IElementHandle;

    public async Task<JsonElement?> EvaluateAsync(string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
            script: expression,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null)
        => (await _channel.EvaluateExpressionHandleAsync(
            script: expression,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false))?.Object;

    public async Task<T> EvaluateAsync<T>(string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
            script: expression,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    public async Task<T> JsonValueAsync<T>() => ScriptsHelper.ParseEvaluateResult<T>(await _channel.JsonValueAsync().ConfigureAwait(false));

    public async Task<IJSHandle> GetPropertyAsync(string propertyName) => (await _channel.GetPropertyAsync(propertyName).ConfigureAwait(false))?.Object;

    public async Task<Dictionary<string, IJSHandle>> GetPropertiesAsync()
    {
        var result = new Dictionary<string, IJSHandle>();
        var channelResult = await _channel.GetPropertiesAsync().ConfigureAwait(false);

        foreach (var kv in channelResult)
        {
            result[kv.Name] = kv.Value.Object;
        }

        return result;
    }

    public async ValueTask DisposeAsync() => await _channel.DisposeAsync().ConfigureAwait(false);

    public override string ToString() => Preview;
}
