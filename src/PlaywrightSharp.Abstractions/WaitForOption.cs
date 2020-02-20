namespace PlaywrightSharp
{
    /// <summary>
    /// Wait for options for <see cref="ClickOptions.WaitFor"/>.
    /// </summary>
    /// <seealso cref="ClickOptions.WaitFor"/>
    /// <seealso cref="FillOptions.WaitFor"/>
    public enum WaitForOption
    {
        /// <summary>
        /// Wait for visible
        /// </summary>
        Visible,

        /// <summary>
        /// Wait for hidden
        /// </summary>
        Hidden,

        /// <summary>
        /// Wait for any
        /// </summary>
        Any,

        /// <summary>
        /// No wait
        /// </summary>
        NoWait,
    }
}
