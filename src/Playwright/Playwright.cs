using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright
{
    [SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
    public static class Playwright
    {
        /// <summary>
        /// Launches Playwright.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync()
        {
            var connection = new Connection();
            var playwright = await connection.WaitForObjectWithKnownNameAsync<PlaywrightImpl>("Playwright").ConfigureAwait(false);
            playwright.Connection = connection;
            return playwright;
        }

        /// <summary>
        /// Runs the CLI tool and performs the browser installation, if one is required.
        /// </summary>
        /// <param name="path">If specified, this path is used to install the browsers. Otherwise, the default is used.</param>
        /// <returns>True if the browsers were installed successfully, or False, if not.</returns>
        public static bool InstallBrowsersIfNeeded(string path = null)
        {
            var p = new Program();
            return p.Run(new[] { "install" }, path) == 0;
        }
    }
}
