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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class JsonPipe : ChannelOwner
{
    private readonly JsonPipeInitializer _initializer;

    public JsonPipe(ChannelOwner parent, string guid, JsonPipeInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
    }

    public event EventHandler<PlaywrightServerMessage> Message;

    public event EventHandler<SerializedError> Closed;

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "closed":
                if (serverParams.Value.TryGetProperty("error", out var error))
                {
                    Closed?.Invoke(this, error.ToObject<SerializedError>(_connection.DefaultJsonSerializerOptions));
                }
                else
                {
                    Closed?.Invoke(this, null);
                }
                break;
            case "message":
                Message?.Invoke(this, serverParams?.GetProperty("message").ToObject<PlaywrightServerMessage>(_connection.DefaultJsonSerializerOptions));
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task CloseAsync() => SendMessageToServerAsync("close");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SendAsync(object message) => SendMessageToServerAsync("send", new Dictionary<string, object>
        {
                { "message", message },
        });
}
