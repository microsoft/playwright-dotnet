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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    internal partial class BrowserContext
    {
        public Task ExposeBindingAsync(string name, Action callback, BrowserContextExposeBindingOptions options = default)
        {
            options ??= new BrowserContextExposeBindingOptions();
            return ExposeBindingAsync(name, callback, handle: options.Handle);
        }

        public Task GrantPermissionsAsync(IEnumerable<string> permissions, BrowserContextGrantPermissionsOptions options = default)
        {
            options ??= new BrowserContextGrantPermissionsOptions();
            return GrantPermissionsAsync(permissions, origin: options.Origin);
        }

        public Task<string> StorageStateAsync(BrowserContextStorageStateOptions options = default)
        {
            options ??= new BrowserContextStorageStateOptions();
            return StorageStateAsync(path: options.Path);
        }
    }
}
