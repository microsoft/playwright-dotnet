namespace PlaywrightSharp
{
    /// <summary>
    /// Options used with <see cref="IPage.SelectAsync(string, SelectOption[])"/>.
    /// </summary>
    public class SelectOption
    {
        /// <summary>
        /// Gets or sets the value option.
        /// </summary>
        /// <remarks>
        /// Matches by <c>option.value</c>.
        /// </remarks>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the label option.
        /// </summary>
        /// <remarks>
        /// Matches by <c>option.label</c>.
        /// </remarks>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the index option.
        /// </summary>
        /// <remarks>
        /// Matches by the index.
        /// </remarks>
        public int Index { get; set; }
    }
}
