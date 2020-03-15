namespace PlaywrightSharp
{
    /// <summary>
    /// Clip data.
    /// </summary>
    /// <seealso cref="ScreenshotOptions.Clip"/>
    public class Clip
    {
        /// <summary>
        /// x-coordinate of top-left corner of clip area.
        /// </summary>
        public decimal X { get; set; }

        /// <summary>
        /// y-coordinate of top-left corner of clip area.
        /// </summary>
        public decimal Y { get; set; }

        /// <summary>
        /// Width of clipping area.
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// Height of clipping area.
        /// </summary>
        public decimal Height { get; set; }

        /// <summary>
        /// Scale of the webpage rendering. Defaults to 1.
        /// </summary>
        public int Scale { get; set; }
    }
}
