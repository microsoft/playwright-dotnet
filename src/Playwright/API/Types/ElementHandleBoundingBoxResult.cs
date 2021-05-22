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

namespace Microsoft.Playwright
{
    public partial class ElementHandleBoundingBoxResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementHandleBoundingBoxResult"/> class.
        /// </summary>
        public ElementHandleBoundingBoxResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementHandleBoundingBoxResult"/> class.
        /// </summary>
        /// <param name="x"><para>the x coordinate of the element in pixels.</para></param>
        /// <param name="y"><para>the y coordinate of the element in pixels.</para></param>
        /// <param name="width"><para>the width of the element in pixels.</para></param>
        /// <param name="height"><para>the height of the element in pixels.</para></param>
        public ElementHandleBoundingBoxResult(float x = 0, float y = 0, float width = 0, float height = 0)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((ElementHandleBoundingBoxResult)obj);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ElementHandleBoundingBoxResult"/> is equal to the current <see cref="ElementHandleBoundingBoxResult"/>.
        /// </summary>
        /// <param name="other">The <see cref="ElementHandleBoundingBoxResult"/> to compare with the current <see cref="ElementHandleBoundingBoxResult"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="ElementHandleBoundingBoxResult"/> is equal to the current
        /// <see cref="ElementHandleBoundingBoxResult"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(ElementHandleBoundingBoxResult other)
            => other != null &&
                other.X == X &&
                other.Y == Y &&
                other.Height == Height &&
                other.Width == Width;

        /// <inheritdoc/>
        public override int GetHashCode()
            => X.GetHashCode() * 397
                ^ Y.GetHashCode() * 397
                ^ Width.GetHashCode() * 397
                ^ Height.GetHashCode() * 397;
    }
}
