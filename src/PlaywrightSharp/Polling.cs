namespace PlaywrightSharp
{
    /// <summary>
    /// An interval at which the <c>pageFunction</c> is executed.
    /// </summary>
    public enum Polling
    {
        /// <summary>
        /// To constantly execute <c>pageFunction</c> in <c>requestAnimationFrame</c> callback.
        /// This is the tightest polling mode which is suitable to observe styling changes.
        /// </summary>
        Raf,
    }
}
