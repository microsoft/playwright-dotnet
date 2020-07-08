using System;

namespace PlaywrightSharp
{
    internal class ViewportSize : IEquatable<ViewportSize>
    {
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The page height in pixels.</value>
        public int Height { get; set; }

        /// <summary>
        /// Clones the <see cref="Viewport"/>.
        /// </summary>
        /// <returns>A copy of the current <see cref="Viewport"/>.</returns>
        public ViewportSize Clone() => (ViewportSize)MemberwiseClone();

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(ViewportSize other)
            => other != null &&
            Width == other.Width &&
            Height == other.Height;

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj) => Equals(obj as Viewport);

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
