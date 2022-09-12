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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core;

internal class Stream : ChannelOwnerBase, IChannelOwner<Stream>, IAsyncDisposable
{
    internal Stream(IChannelOwner parent, string guid) : base(parent, guid)
    {
        Channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => Channel;

    IChannel<Stream> IChannelOwner<Stream>.Channel => Channel;

    public StreamChannel Channel { get; }

    public StreamImpl StreamImpl => new(this);

    public Task<byte[]> ReadAsync(int size) => Channel.ReadAsync(size);

    public ValueTask DisposeAsync() => new ValueTask(CloseAsync());

    public Task CloseAsync() => Channel.CloseAsync();
}

internal class StreamImpl : System.IO.Stream
{
    private readonly Stream _stream;

    internal StreamImpl(Stream stream)
    {
        _stream = stream;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => throw new NotImplementedException();

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush() => throw new NotImplementedException();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var result = await _stream.ReadAsync(count).ConfigureAwait(false);
        result.CopyTo(buffer, offset);
        return result.Length;
    }

    public override void Close() => _stream.CloseAsync().ConfigureAwait(false);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}
