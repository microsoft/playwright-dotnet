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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class LocalUtils : ChannelOwnerBase
{
    internal readonly Dictionary<string, BrowserNewContextOptions> _devices = new();

    public LocalUtils(ChannelOwnerBase parent, string guid, LocalUtilsInitializer initializer) : base(parent, guid)
    {
        foreach (var entry in initializer.DeviceDescriptors)
        {
            _devices[entry.Name] = entry.Descriptor;
        }
    }

    internal Task ZipAsync(string zipFile, List<NameValue> entries, string mode, string stacksId, bool includeSources)
        => SendMessageToServerAsync("zip", new Dictionary<string, object>
        {
                { "zipFile", zipFile },
                { "entries", entries },
                { "mode", mode },
                { "stacksId", stacksId },
                { "includeSources", includeSources },
        });

    internal async Task<(string HarId, string Error)> HarOpenAsync(string file)
    {
        var response = await SendMessageToServerAsync("harOpen", new Dictionary<string, object>
            {
                  { "file", file },
            }).ConfigureAwait(false);
        return (response.GetString("harId", true), response.GetString("error", true));
    }

    internal Task<LocalUtilsHarLookupResult> HarLookupAsync(
        string harId,
        string url,
        string method,
        List<Header> headers,
        byte[] postData,
        bool isNavigationRequest)
        => SendMessageToServerAsync<LocalUtilsHarLookupResult>("harLookup", new Dictionary<string, object>
            {
                { "harId", harId },
                { "url", url },
                { "method", method },
                { "headers", headers },
                { "postData", postData != null ? Convert.ToBase64String(postData) : null },
                { "isNavigationRequest", isNavigationRequest },
            });

    internal Task HarCloseAsync(string harId)
         => SendMessageToServerAsync("HarCloseAsync", new Dictionary<string, object>
        {
                  { "harId", harId },
        });

    internal Task HarUnzipAsync(string zipFile, string harFile)
         => SendMessageToServerAsync("harUnzip", new Dictionary<string, object>
        {
                  { "zipFile", zipFile },
                  { "harFile", harFile },
        });

    internal async Task<JsonPipe> ConnectAsync(string wsEndpoint = default, IEnumerable<KeyValuePair<string, string>> headers = default, float? slowMo = default, float? timeout = default, string exposeNetwork = default)
         => (await SendMessageToServerAsync("connect", new Dictionary<string, object>
            {
                { "wsEndpoint", wsEndpoint },
                { "headers", headers },
                { "slowMo", slowMo },
                { "timeout", timeout },
                { "exposeNetwork", exposeNetwork },
            }).ConfigureAwait(false)).Value.GetObject<JsonPipe>("pipe", _connection);

    internal void AddStackToTracingNoReply(List<StackFrame> stack, int id)
         => SendMessageToServerAsync("addStackToTracingNoReply", new Dictionary<string, object>
        {
            {
                "callData", new ClientSideCallMetadata()
                {
                    Id = id,
                    Stack = stack,
                }
            },
        }).IgnoreException();

    internal Task TraceDiscardedAsync(string stacksId)
        => SendMessageToServerAsync("traceDiscarded", new Dictionary<string, object>
        {
            { "stacksId", stacksId },
        });

    internal async Task<string> TracingStartedAsync(string tracesDir, string traceName)
    {
        var response = await SendMessageToServerAsync("tracingStarted", new Dictionary<string, object>
        {
            { "tracesDir", tracesDir },
            { "traceName", traceName },
        }).ConfigureAwait(false);
        return response.GetString("stacksId", true);
    }
}

internal class LocalUtilsHarLookupResult
{
    public string Action { get; set; }

    public string Message { get; set; }

    public string RedirectURL { get; set; }

    public int Status { get; set; }

    public List<NameValue> Headers { get; set; }

    public byte[] Body { get; set; }
}
