using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// View port data.
    /// </summary>
    public class Viewport : IEquatable<Viewport>
    {
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The page width width in pixels.</value>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The page height in pixels.</value>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets whether the meta viewport tag is taken into account.
        /// </summary>
        /// <value>Whether the meta viewport tag is taken into account. Defaults to <c>false</c>.</value>
        public bool IsMobile { get; set; }

        /// <summary>
        /// Gets or sets the device scale factor.
        /// </summary>
        /// <value>Specify device scale factor (can be thought of as dpr).</value>
        public double DeviceScaleFactor { get; set; } = 1;

        /// <summary>
        /// Clones the <see cref="Viewport"/>.
        /// </summary>
        /// <returns>A copy of the current <see cref="Viewport"/>.</returns>
        public Viewport Clone() => (Viewport)MemberwiseClone();

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(Viewport other)
            => other != null &&
            Width == other.Width &&
            Height == other.Height &&
            IsMobile == other.IsMobile &&
            DeviceScaleFactor == other.DeviceScaleFactor;

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj) => Equals(obj as Viewport);

#if NETSTANDARD2_1
        /// <inheritdoc cref="object.GetHashCode()"/>
        public override int GetHashCode() => HashCode.Combine(Width, Height, IsMobile, DeviceScaleFactor);
#else
        /// <inheritdoc cref="object.GetHashCode()"/>
        public override int GetHashCode()
            => 711844102
                ^ Width.GetHashCode()
                ^ Height.GetHashCode()
                ^ IsMobile.GetHashCode()
                ^ DeviceScaleFactor.GetHashCode();
#endif
    }
}
