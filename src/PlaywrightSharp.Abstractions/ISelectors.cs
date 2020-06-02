using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Selectors can be used to install custom selector engines.
    /// </summary>
    public interface ISelectors
    {
        /// <summary>
        /// Gets the version of the selectors.
        /// </summary>
        int Generation { get; }

        /// <summary>
        /// Gets the selectors sources.
        /// </summary>
        IEnumerable<string> Sources { get; }

        /// <summary>
        /// Installs a custom selector engine.
        /// </summary>
        /// <param name="engineFunction">Function that evaluates to a selector engine instance.</param>
        /// <param name="args">Arguments to pass to <paramref name="engineFunction"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the selector engine is registerd.</returns>
        Task RegisterAsync(string engineFunction, params object[] args);

        /// <summary>
        /// Returns the selector for <paramref name="elementHandle"/>.
        /// </summary>
        /// <param name="name">The name of the engine to use.</param>
        /// <param name="elementHandle">The element.</param>
        /// <returns>A <see cref="Task"/> that completes when the element's selector is calculated, yielding the element's selector.</returns>
        Task<string> CreateSelectorAsync(string name, IElementHandle elementHandle);
    }
}
