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

namespace Microsoft.Playwright
{
    /// <summary>
    /// View port data.
    /// </summary>
    public partial class ViewportSize : IEquatable<ViewportSize>
    {
        /// <summary>
        /// ViewportSize used to determine if the a Viewport was set or not.
        /// </summary>
        public static ViewportSize Default => new() { Height = 720, Width = 1280 };

        /// <summary>
        /// Disables the viewport.
        /// </summary>
        public static ViewportSize NoViewport => new() { Height = -1, Width = -1 };

        /// <summary>
        /// Clones the <see cref="ViewportSize"/>.
        /// </summary>
        /// <returns>A copy of the current <see cref="ViewportSize"/>.</returns>
        public ViewportSize Clone() => (ViewportSize)MemberwiseClone();

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(ViewportSize other)
            => other != null &&
            Width == other.Width &&
            Height == other.Height;

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj) => Equals(obj as ViewportSize);

#if NETSTANDARD2_1
        /// <inheritdoc cref="object.GetHashCode()"/>
        public override int GetHashCode() => HashCode.Combine(Width, Height);
#else
        /// <inheritdoc cref="object.GetHashCode()"/>
        public override int GetHashCode()
            => 711844102
                ^ Width.GetHashCode()
                ^ Height.GetHashCode();
#endif
    }
}
