namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="IElementHandle.WaitForElementStateAsync(ElementState, int?)"/>.
    /// </summary>
    public enum ElementState
    {
        /// <summary>
        /// Element visible.
        /// </summary>
        /// <remarks>
        /// Element is considered visible when it has non-empty bounding box and does not have visibility:hidden computed style.
        /// Note that elements of zero size or with display:none are not considered visible.
        /// </remarks>
        Visible,

        /// <summary>
        /// Not <see cref="Visible"/> or not attached.
        /// </summary>
        Hidden,

        /// <summary>
        /// <see cref="Visible"/> and Stable.
        /// </summary>
        /// <remarks>
        /// Element is considered stable when it has maintained the same bounding box for at least two consecutive animation frames.
        /// </remarks>
        Stable,

        /// <summary>
        /// Element enabled.
        /// </summary>
        /// <remarks>
        /// Element is considered enabled when it is not a button, select or input with a disabled property set.
        /// </remarks>
        Enabled,

        /// <summary>
        /// Element not <see cref="Enabled"/>.
        /// </summary>
        Disabled,

        /// <summary>
        /// Wait until the element is editable.
        /// </summary>
        Editable,
    }
}
