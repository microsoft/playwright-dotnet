namespace PlaywrightSharp
{
    /// <summary>
    /// Represents a definition of a keyboard key.
    /// </summary>
    public class KeyDefinition
    {
        /// <summary>
        /// Gets or sets the key code.
        /// </summary>
        public int? KeyCode { get; set; }

        /// <summary>
        /// Gets or sets the key code without location.
        /// </summary>
        public int? KeyCodeWithoutLocation { get; set; }

        /// <summary>
        /// Gets or sets the shift key code.
        /// </summary>
        public int? ShiftKeyCode { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the shift key.
        /// </summary>
        public string ShiftKey { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the shift text.
        /// </summary>
        public string ShiftText { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public int? Location { get; set; }

        /// <summary>
        /// Gets or sets the windows virtual key code.
        /// </summary>
        public int? WindowsVirtualKeyCode { get; set; }
    }
}
