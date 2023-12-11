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
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Dialog : ChannelOwner, IDialog
{
    private readonly DialogInitializer _initializer;

    public Dialog(ChannelOwner parent, string guid, DialogInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
    }

    public string Type => _initializer.Type;

    public string DefaultValue => _initializer.DefaultValue;

    public string Message => _initializer.Message;

    // Note: dialogs that open early during page initialization block it.
    // Therefore, we must report the dialog without a page to be able to handle it.
    public IPage Page => _initializer.Page;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task AcceptAsync(string promptText) => SendMessageToServerAsync(
            "accept",
            new Dictionary<string, object>
            {
                ["promptText"] = promptText ?? string.Empty,
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task DismissAsync() => SendMessageToServerAsync("dismiss");
}
