using System.Threading.Tasks;

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

        /// <summary>
        /// Terget's <see cref="IPage"/>.
        /// </summary>
        /// <remarks>
        /// If the target is not of type <see cref="TargetType.Page"/> or <see cref="TargetType.BackgroundPage"/>, returns null.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the corresponding <see cref="IPage"/> is found, yielding its <see cref="IPage"/>.</returns>
        Task<IPage> GetPageAsync();
    }
}
