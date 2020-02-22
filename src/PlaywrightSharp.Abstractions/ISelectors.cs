using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Selectors can be used to install custom selector engines.
    /// </summary>
    public interface ISelectors
    {
        /// <summary>
        /// Installs a custom selector engine.
        /// </summary>
        /// <param name="engineFunction">Function that evaluates to a selector engine instance.</param>
        /// <param name="args">Arguments to pass to <paramref name="engineFunction"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the selector engine is registerd.</returns>
        Task RegisterAsync(string engineFunction, params object[] args);
    }
}
