/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Copyright (c) 2020 Meir Blachman
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core
{
    internal class FrameLocator : IFrameLocator
    {
        private readonly Frame _frame;
        private readonly string _frameSelector;

        public FrameLocator(Frame parent, string selector)
        {
            _frame = parent;
            _frameSelector = selector;
        }

        IFrameLocator IFrameLocator.First => new FrameLocator(_frame, $"{_frameSelector} >> nth=0");

        IFrameLocator IFrameLocator.Last => new FrameLocator(_frame, $"{_frameSelector} >> nth=-1");

        IFrameLocator IFrameLocator.FrameLocator(string selector) => new FrameLocator(_frame, $"{_frameSelector} >> control=enter-frame >> {selector}");

        ILocator IFrameLocator.Locator(string selector, FrameLocatorLocatorOptions options) => new Locator(_frame, $"{_frameSelector} >> control=enter-frame >> {selector}", new() { HasTextRegex = options?.HasTextRegex, HasTextString = options?.HasTextString });

        IFrameLocator IFrameLocator.Nth(int index) => new FrameLocator(_frame, $"{_frameSelector} >> nth={index}");
    }
}
