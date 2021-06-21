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

using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal partial class Mouse : IMouse
    {
        private readonly PageChannel _channel;

        public Mouse(PageChannel channel)
        {
            _channel = channel;
        }

        public Task ClickAsync(float x, float y, MouseButton? button, int? clickCount, float? delay)
            => _channel.MouseClickAsync(x, y, delay, button, clickCount);

        public Task DblClickAsync(float x, float y, MouseButton? button, float? delay)
            => _channel.MouseClickAsync(x, y, delay, button, 2);

        public Task DownAsync(MouseButton? button, int? clickCount)
            => _channel.MouseDownAsync(button, clickCount);

        public Task MoveAsync(float x, float y, int? steps)
            => _channel.MouseMoveAsync(x, y, steps ?? 1);

        public Task UpAsync(MouseButton? button, int? clickCount)
            => _channel.MouseUpAsync(button, clickCount);
    }
}
