namespace PlaywrightSharp.Input
{
    /// <summary>
    /// The type of button click to use with <see cref="IPage.ClickAsync(string, int, MouseButton, int, Modifier[], System.Drawing.Point?, int?, bool, bool)"/> or <see cref="IFrame.ClickAsync(string, int, MouseButton, int, Modifier[], System.Drawing.Point?, int?, bool, bool)"/>.
    /// </summary>
    public enum MouseButton
    {
        /// <summary>
        /// Non specified.
        /// </summary>
        None,

        /// <summary>
        /// The left mouse button.
        /// </summary>
        Left,

        /// <summary>
        /// The right mouse button.
        /// </summary>
        Right,

        /// <summary>
        /// The middle mouse button.
        /// </summary>
        Middle,
    }
}
