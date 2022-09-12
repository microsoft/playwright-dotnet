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
using System.Linq;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class StorageState : IEquatable<StorageState>
{
    /// <summary>
    /// Cookie list.
    /// </summary>
    public ICollection<Cookie> Cookies { get; set; } = new List<Cookie>();

    /// <summary>
    /// List of local storage per origin.
    /// </summary>
    public ICollection<OriginStorage> Origins { get; set; } = new List<OriginStorage>();

    public bool Equals(StorageState other)
        => other != null &&
            Cookies.SequenceEqual(other.Cookies) &&
            Origins.SequenceEqual(other.Origins);

    public override int GetHashCode()
        => 412870874 +
            EqualityComparer<ICollection<Cookie>>.Default.GetHashCode(Cookies) +
            EqualityComparer<ICollection<OriginStorage>>.Default.GetHashCode(Origins);

    public override bool Equals(object obj) => Equals(obj as StorageState);
}
