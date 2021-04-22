using System;

namespace PlaywrightSharp
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
