namespace PlaywrightSharp
{
    /// <summary>
    /// View port data.
    /// </summary>
    public class Viewport
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
    }
}
