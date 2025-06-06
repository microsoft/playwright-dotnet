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
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core;

internal class Mouse : IMouse
{
    private readonly Page _page;

    public Mouse(Page page)
    {
        _page = page;
    }

    public Task ClickAsync(float x, float y, MouseClickOptions? options = default)
        => _page.SendMessageToServerAsync(
            "mouseClick",
            new Dictionary<string, object?>
            {
                ["x"] = x,
                ["y"] = y,
                ["delay"] = options?.Delay,
                ["button"] = options?.Button,
                ["clickCount"] = options?.ClickCount,
            });

    public async Task DblClickAsync(float x, float y, MouseDblClickOptions? options = default)
    {
        await _page.WrapApiCallAsync(
            async () =>
        {
            await _page.SendMessageToServerAsync(
                "mouseClick",
                new Dictionary<string, object?>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["delay"] = options?.Delay,
                    ["button"] = options?.Button,
                    ["clickCount"] = 2,
                }).ConfigureAwait(false);
        },
            false,
            "Double click").ConfigureAwait(false);
    }

    public Task DownAsync(MouseDownOptions? options = default)
        => _page.SendMessageToServerAsync(
            "mouseDown",
            new Dictionary<string, object?>
            {
                ["button"] = options?.Button,
                ["clickCount"] = options?.ClickCount,
            });

    public Task MoveAsync(float x, float y, MouseMoveOptions? options = default)
        => _page.SendMessageToServerAsync(
            "mouseMove",
            new Dictionary<string, object?>
            {
                ["x"] = x,
                ["y"] = y,
                ["steps"] = options?.Steps,
            });

    public Task UpAsync(MouseUpOptions? options = default)
      => _page.SendMessageToServerAsync(
            "mouseUp",
            new Dictionary<string, object?>
            {
                ["button"] = options?.Button,
                ["clickCount"] = options?.ClickCount,
            });

    public Task WheelAsync(float deltaX, float deltaY)
        => _page.SendMessageToServerAsync(
            "mouseWheel",
            new Dictionary<string, object?>
            {
                ["deltaX"] = deltaX,
                ["deltaY"] = deltaY,
            });
}
