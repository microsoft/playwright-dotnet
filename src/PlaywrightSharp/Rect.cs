using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Bounding box data returned by <see cref="IElementHandle.GetBoundingBoxAsync"/>.
    /// </summary>
    public class Rect : IEquatable<Rect>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> class.
        /// </summary>
        public Rect()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect"/> class.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public Rect(decimal x, decimal y, decimal width, decimal height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The x coordinate of the element in pixels.
        /// </summary>
        public decimal X { get; set; }

        /// <summary>
        /// The y coordinate of the element in pixels.
        /// </summary>
        public decimal Y { get; set; }

        /// <summary>
        /// The width of the element in pixels.
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// The height of the element in pixels.
        /// </summary>
        public decimal Height { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((Rect)obj);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Rect"/> is equal to the current <see cref="Rect"/>.
        /// </summary>
        /// <param name="other">The <see cref="Rect"/> to compare with the current <see cref="Rect"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="Rect"/> is equal to the current
        /// <see cref="Rect"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(Rect other)
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
