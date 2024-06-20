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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Core;

internal class WritableStream : ChannelOwner, IAsyncDisposable
{
    internal WritableStream(ChannelOwner parent, string guid) : base(parent, guid)
    {
    }

    public WritableStreamImpl WritableStreamImpl => new(this);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task WriteAsync(string binary) => SendMessageToServerAsync(
            "write",
            new Dictionary<string, object>
            {
                ["binary"] = binary,
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ValueTask DisposeAsync() => new ValueTask(CloseAsync());

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task CloseAsync() => SendMessageToServerAsync("close");
}

internal class WritableStreamImpl : System.IO.Stream
{
    private readonly WritableStream _stream;

    internal WritableStreamImpl(WritableStream stream)
    {
        _stream = stream;
    }

    public override bool CanRead => throw new NotImplementedException();

    public override bool CanSeek => throw new NotImplementedException();

    public override bool CanWrite => true;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush() => throw new NotImplementedException();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public override void Close()
    {
        _ = _stream.CloseAsync().ConfigureAwait(false);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _stream.WriteAsync(Convert.ToBase64String(buffer.Skip(offset).Take(count).ToArray())).ConfigureAwait(false);
    }
}
