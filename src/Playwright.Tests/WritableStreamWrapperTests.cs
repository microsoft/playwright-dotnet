/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Text.Json;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Tests;

public class WritableStreamWrapperTests
{
    private WritableStreamWrapper _instance;

    [SetUp]
    public void SetUp()
    {
        Connection conn = new ();
        conn.OnMessage = (_, _) => Task.CompletedTask;
        Root owner = new (null, conn, "");
        WritableStream writableStream = new WritableStream(owner, "");

        _instance = new(writableStream);
    }

    [Test]
    public void ShouldPresentValidWriteOnlyState()
    {
        Assert.IsTrue(_instance.CanWrite);
        Assert.IsFalse(_instance.CanRead);
        Assert.IsFalse(_instance.CanSeek);

        Assert.Throws<NotSupportedException>(() => { _ = _instance.Length; });
        Assert.Throws<NotSupportedException>(() => { _ = _instance.Position; });
        Assert.Throws<NotSupportedException>(() => { _instance.Position = 10; });

        Assert.Throws<NotSupportedException>(() => { _instance.Read(null, 0, 0); });

        Assert.Throws<NotSupportedException>(() => { _instance.Seek(0, SeekOrigin.Begin); });
        Assert.Throws<NotSupportedException>(() => { _instance.SetLength(10); });
    }

    [Test]
    public void ShouldSupportFlush()
    {
        Assert.DoesNotThrow(() => _instance.Flush());
    }
}
