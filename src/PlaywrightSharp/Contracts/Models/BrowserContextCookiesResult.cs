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
 *
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

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="BrowserContextCookiesResult"/>.
    /// </summary>
    public partial class BrowserContextCookiesResult : IEquatable<BrowserContextCookiesResult>
    {
        /// <inheritdoc cref="IEquatable{T}"/>
        public bool Equals(BrowserContextCookiesResult other)
            => other != null &&
                Name == other.Name &&
                Value == other.Value &&
                Domain == other.Domain &&
                Path == other.Path &&
                Expires == other.Expires &&
                HttpOnly == other.HttpOnly &&
                Secure == other.Secure &&
                SameSite == other.SameSite;

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as BrowserContextCookiesResult);

        /// <inheritdoc/>
        public override int GetHashCode()
            => 412870874 +
                EqualityComparer<string>.Default.GetHashCode(Name) +
                EqualityComparer<string>.Default.GetHashCode(Value) +
                EqualityComparer<string>.Default.GetHashCode(Domain) +
                EqualityComparer<string>.Default.GetHashCode(Path) +
                EqualityComparer<float>.Default.GetHashCode(Expires) +
                EqualityComparer<bool>.Default.GetHashCode(HttpOnly) +
                EqualityComparer<bool>.Default.GetHashCode(Secure) +
                EqualityComparer<SameSiteAttribute>.Default.GetHashCode(SameSite);
    }
}
