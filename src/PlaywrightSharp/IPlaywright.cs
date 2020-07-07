using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// IPlaywright provides methods to interact with the playwright server.
    /// </summary>
    public interface IPlaywright
    {
        /// <summary>
        /// Gets the Chromium browser type from the playwright server.
        /// </summary>
        IBrowserType Chromium { get; }

        /// <summary>
        /// Gets the Firefox browser type from the playwright server.
        /// </summary>
        IBrowserType Firefox { get; }

        /// <summary>
        /// Gets the Webkit browser type from the playwright server.
        /// </summary>
        IBrowserType Webkit { get; }
    }
}
