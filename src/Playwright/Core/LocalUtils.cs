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

internal class LocalUtils : ChannelOwnerBase, IChannelOwner<LocalUtils>
{
    private readonly LocalUtilsChannel _channel;

    public LocalUtils(IChannelOwner parent, string guid, JsonElement? initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<LocalUtils> IChannelOwner<LocalUtils>.Channel => _channel;

    internal Task ZipAsync(string zipFile, List<NameValue> entries)
        => _channel.ZipAsync(zipFile, entries);

    internal Task<(string HarId, string Error)> HarOpenAsync(string file)
        => _channel.HarOpenAsync(file);

    internal Task<LocalUtilsHarLookupResult> HarLookupAsync(
        string harId,
        string url,
        string method,
        List<Header> headers,
        byte[] postData,
        bool isNavigationRequest)
        => _channel.HarLookupAsync(harId, url, method, headers, postData, isNavigationRequest);

    internal Task HarCloseAsync(string harId)
         => _channel.HarCloseAsync(harId);

    internal Task HarUnzipAsync(string zipFile, string harFile)
         => _channel.HarUnzipAsync(zipFile, harFile);

    internal Task<JsonPipeChannel> ConnectAsync(string wsEndpoint = default, IEnumerable<KeyValuePair<string, string>> headers = default, float? slowMo = default, float? timeout = default)
         => _channel.ConnectAsync(wsEndpoint, headers, slowMo, timeout);
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
