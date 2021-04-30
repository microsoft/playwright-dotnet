namespace Microsoft.Playwright
{
    /// <summary>
    /// Three-state boolean.
    /// </summary>
    public enum PressedState
    {
        /// <summary>
        /// Pressed.
        /// </summary>
        Pressed = 0,

        /// <summary>
        /// Released.
        /// </summary>
        Released,

        /// <summary>
        /// Mixed.
        /// </summary>
        Mixed,
    }
}
