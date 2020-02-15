namespace PlaywrightSharp
{
    /// <summary>
    /// Target class.
    /// </summary>
    public interface ITarget
    {
        /// <summary>
        /// Gets the URL.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets the type. It will be <see cref="ITarget.Type"/>.
        /// </summary>
        TargetType Type { get; }
    }
}
