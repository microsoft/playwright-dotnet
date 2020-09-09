namespace PlaywrightSharp
{
    /// <summary>
    /// Three-state boolean. See <seealso cref="SerializedAXNode.Pressed"/>.
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
