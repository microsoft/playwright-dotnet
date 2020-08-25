using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// View port data.
    /// </summary>
    public class ViewportSize : IEquatable<ViewportSize>
    {
        /// <summary>
        /// Viewport width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Viewport height.
        /// </summary>
        public int Height { get; set; }

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
