/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright.Core
{
    internal partial class Mouse
    {
        public Task ClickAsync(float x, float y, MouseClickOptions? options = default)
        {
            options ??= new();
            return ClickAsync(x, y, button: options.Button, clickCount: options.ClickCount, delay: options.Delay);
        }

        public Task DblClickAsync(float x, float y, MouseDblClickOptions? options = default)
        {
            options ??= new();
            return DblClickAsync(x, y, button: options.Button, delay: options.Delay);
        }

        public Task DownAsync(MouseDownOptions? options = default)
        {
            options ??= new();
            return DownAsync(button: options.Button, clickCount: options.ClickCount);
        }

        public Task MoveAsync(float x, float y, MouseMoveOptions? options = default)
        {
            options ??= new();
            return MoveAsync(x, y, steps: options.Steps);
        }

        public Task UpAsync(MouseUpOptions? options = default)
        {
            options ??= new();
            return UpAsync(button: options.Button, clickCount: options.ClickCount);
        }
    }
}

#nullable disable
