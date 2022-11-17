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

namespace Microsoft.Playwright.Transport.Channels;

internal class SelectorsChannel : Channel<Selectors>
{
    public SelectorsChannel(string guid, Connection connection, Selectors owner) : base(guid, connection, owner)
    {
    }

    internal Task RegisterAsync(SelectorsRegisterParams @params)
        => Connection.SendMessageToServerAsync<JsonElement>(
            Guid,
            "register",
            new Dictionary<string, object>
            {
                ["name"] = @params.Name,
                ["source"] = @params.Source,
                ["contentScript"] = @params.ContentScript,
            });

    internal Task SetTestIdAttributeAsync(string name)
        => Connection.SendMessageToServerAsync<JsonElement>(
            Guid,
            "setTestIdAttributeName",
            new Dictionary<string, object>
            {
                ["testIdAttributeName"] = name,
            });
}

internal record SelectorsRegisterParams
{
    internal string Name;
    internal string Source;
    internal bool? ContentScript;
}
