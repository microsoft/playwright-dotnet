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
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels;

internal class TracingChannel : Channel<Tracing>
{
    public TracingChannel(string guid, Connection connection, Tracing owner) : base(guid, connection, owner)
    {
    }

    internal Task TracingStartAsync(string name, string title, bool? screenshots, bool? snapshots, bool? sources)
        => Connection.SendMessageToServerAsync(
            Object,
            "tracingStart",
            new Dictionary<string, object>
            {
                ["name"] = name,
                ["title"] = title,
                ["screenshots"] = screenshots,
                ["snapshots"] = snapshots,
                ["sources"] = sources,
            });

    internal Task TracingStopAsync()
        => Connection.SendMessageToServerAsync(
            Object,
            "tracingStop");

    internal async Task<string> StartChunkAsync(string title = null, string name = null)
        => (await Connection.SendMessageToServerAsync(Object, "tracingStartChunk", new Dictionary<string, object>
        {
            ["title"] = title,
            ["name"] = name,
        }).ConfigureAwait(false))?.GetProperty("traceName").ToString();

    internal async Task<(Artifact Artifact, List<NameValue> Entries)> TracingStopChunkAsync(string mode)
    {
        var result = await Connection.SendMessageToServerAsync(Object, "tracingStopChunk", new Dictionary<string, object>
        {
            ["mode"] = mode,
        }).ConfigureAwait(false);

        var artifact = result.GetObject<Artifact>("artifact", Connection);
        List<NameValue> entries = new();
        if (result.Value.TryGetProperty("entries", out var entriesElement))
        {
            foreach (var current in entriesElement.EnumerateArray())
            {
                entries.Add(current.Deserialize<NameValue>());
            }
        }
        return (artifact, entries);
    }
}
