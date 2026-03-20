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

namespace Microsoft.Playwright.Core;

internal class Debugger : ChannelOwner, IDebugger
{
    private List<PausedDetail> _pausedDetails = new();

    public Debugger(ChannelOwner parent, string guid) : base(parent, guid)
    {
    }

    public event EventHandler? PausedStateChanged;

    public IReadOnlyList<PausedDetail> PausedDetails => _pausedDetails;

    internal override void OnMessage(string method, JsonElement serverParams)
    {
        switch (method)
        {
            case "pausedStateChanged":
                _pausedDetails = serverParams.GetProperty("pausedDetails").ToObject<List<PausedDetail>>(_connection.DefaultJsonSerializerOptions);
                PausedStateChanged?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task PauseAsync() => SendMessageToServerAsync("pause");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ResumeAsync() => SendMessageToServerAsync("resume");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task NextAsync() => SendMessageToServerAsync("next");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RunToAsync(Location location) => SendMessageToServerAsync("runTo", new Dictionary<string, object?>
    {
        ["location"] = location,
    });
}
